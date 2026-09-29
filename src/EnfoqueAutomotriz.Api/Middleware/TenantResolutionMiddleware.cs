using Npgsql;
using EnfoqueAutomotriz.Api.Extensions;

namespace EnfoqueAutomotriz.Api.Middleware;

/// <summary>
/// Resuelve el tenant (taller) activo para el request actual: header, subdominio, path prefix
/// o slug por defecto (modo single-tenant/dev). Valida estado de la suscripción en master.tenants.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly IConfiguration _config;

    // Rutas que no requieren resolución de tenant
    private static readonly HashSet<string> BypassPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/swagger", "/favicon.ico"
    };

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext ctx, TenantContext tenantCtx)
    {
        if (BypassPaths.Any(p => ctx.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(ctx);
            return;
        }

        var slug = ResolveTenantSlug(ctx);

        if (string.IsNullOrEmpty(slug))
        {
            slug = _config["TenantSettings:DefaultSlug"];
            if (string.IsNullOrEmpty(slug))
            {
                await _next(ctx);
                return;
            }
        }

        var connStr = DatabaseExtensions.BuildConnectionString(_config);
        if (string.IsNullOrEmpty(connStr))
        {
            await _next(ctx);
            return;
        }

        try
        {
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ctx.RequestAborted);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT schema_name, estado, fecha_vencimiento FROM master.tenants WHERE slug = $1 LIMIT 1";
            cmd.Parameters.AddWithValue(slug);

            await using var reader = await cmd.ExecuteReaderAsync(ctx.RequestAborted);
            if (await reader.ReadAsync(ctx.RequestAborted))
            {
                var schemaName = reader.GetString(0);
                var estado = reader.GetString(1);
                var fechaVencimiento = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);

                if (estado == "activo" && fechaVencimiento.HasValue && fechaVencimiento.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Tenant '{Slug}' venció el {Fecha}, auto-suspendiendo", slug, fechaVencimiento.Value);
                    estado = "suspendido";

                    try
                    {
                        await using var updateCmd = conn.CreateCommand();
                        updateCmd.CommandText = "UPDATE master.tenants SET estado = 'suspendido', updated_at = NOW() WHERE slug = $1 AND estado = 'activo'";
                        updateCmd.Parameters.AddWithValue(slug);
                        await updateCmd.ExecuteNonQueryAsync(ctx.RequestAborted);
                    }
                    catch { /* no bloquear el request si falla el update */ }
                }

                if (estado == "suspendido")
                {
                    ctx.Response.StatusCode = 402;
                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        error = "Cuenta suspendida. Contacte a soporte para regularizar su pago.",
                        tenant = slug,
                        vencimiento = fechaVencimiento
                    });
                    return;
                }

                if (estado == "cancelado")
                {
                    ctx.Response.StatusCode = 403;
                    await ctx.Response.WriteAsJsonAsync(new { error = "Cuenta cancelada.", tenant = slug });
                    return;
                }

                tenantCtx.Set(slug, schemaName);
                _logger.LogDebug("Tenant resuelto: {Slug} → schema {Schema}", slug, schemaName);
            }
            else
            {
                _logger.LogWarning("Tenant no encontrado: {Slug}", slug);
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsJsonAsync(new { error = $"Tenant '{slug}' no encontrado." });
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolviendo tenant {Slug}", slug);
            // Modo degradado: continúa sin tenant (single-tenant fallback)
        }

        await _next(ctx);
    }

    private static string? ResolveTenantSlug(HttpContext ctx)
    {
        // 1. Header explícito (útil en desarrollo y APIs internas)
        if (ctx.Request.Headers.TryGetValue("X-Tenant-Slug", out var headerSlug)
            && !string.IsNullOrWhiteSpace(headerSlug))
            return headerSlug.ToString().ToLowerInvariant().Trim();

        // 2. Subdominio: tallerxyz.enfoqueautomotriz.app → slug = "tallerxyz"
        var host = ctx.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length >= 3)
        {
            var subdomain = parts[0].ToLowerInvariant();
            if (subdomain is not ("www" or "api" or "app" or "admin" or "localhost"))
                return subdomain;
        }

        // 3. Path prefix: /t/tallerxyz/api/... → slug = "tallerxyz"
        var path = ctx.Request.Path.Value ?? "";
        if (path.StartsWith("/t/", StringComparison.OrdinalIgnoreCase))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
                return segments[1].ToLowerInvariant();
        }

        return null;
    }
}
