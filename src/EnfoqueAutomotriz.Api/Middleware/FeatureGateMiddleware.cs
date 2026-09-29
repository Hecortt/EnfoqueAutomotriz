using Npgsql;
using EnfoqueAutomotriz.Api.Extensions;

namespace EnfoqueAutomotriz.Api.Middleware;

/// <summary>
/// Bloquea requests a endpoints de módulos que el tenant (taller) no tiene contratados
/// según su plan. Lee master.feature_flags y compara contra la ruta solicitada.
/// Rutas no mapeadas (auth, catalogs, health, etc.) pasan sin restricción.
/// </summary>
public sealed class FeatureGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FeatureGateMiddleware> _logger;

    // Mapeo: segmento de ruta API → feature flag requerido.
    // Se completará conforme se agreguen módulos de negocio del taller
    // (ordenes-servicio, inventario-refacciones, citas, etc.).
    private static readonly Dictionary<string, string> RouteFeatureMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "reportes", "reportes" },
    };

    // Rutas que nunca se bloquean (infraestructura, auth, config)
    private static readonly HashSet<string> BypassSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "auth", "health", "diag", "swagger", "feature-flags", "saas-admin",
        "catalogs", "configuracion-empresa", "notificaciones", "permisos-rol"
    };

    public FeatureGateMiddleware(RequestDelegate next, ILogger<FeatureGateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx, ITenantContext tenant, IConfiguration config)
    {
        if (!tenant.IsResolved)
        {
            await _next(ctx);
            return;
        }

        var routeSegment = ExtractRouteSegment(ctx.Request.Path.Value);

        if (string.IsNullOrEmpty(routeSegment)
            || BypassSegments.Contains(routeSegment)
            || !RouteFeatureMap.TryGetValue(routeSegment, out var requiredFeature))
        {
            await _next(ctx);
            return;
        }

        var connStr = DatabaseExtensions.BuildConnectionString(config);
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
            cmd.CommandText = @"
                SELECT COALESCE(
                    (SELECT ff.habilitado
                     FROM master.feature_flags ff
                     INNER JOIN master.tenants t ON t.id = ff.tenant_id
                     WHERE t.slug = $1 AND ff.feature = $2),
                    FALSE
                )";
            cmd.Parameters.AddWithValue(tenant.Slug);
            cmd.Parameters.AddWithValue(requiredFeature);

            var habilitado = (bool)(await cmd.ExecuteScalarAsync(ctx.RequestAborted))!;

            if (!habilitado)
            {
                _logger.LogWarning(
                    "Feature '{Feature}' bloqueado para tenant '{Tenant}' (ruta: {Path})",
                    requiredFeature, tenant.Slug, ctx.Request.Path);

                ctx.Response.StatusCode = 403;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    error = $"Módulo '{requiredFeature}' no incluido en su plan actual.",
                    upgrade = "Contacte a soporte para actualizar su plan.",
                    feature = requiredFeature
                });
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando feature gate para tenant '{Tenant}'", tenant.Slug);
            // Fail open para no bloquear en caso de error de BD
        }

        await _next(ctx);
    }

    private static string? ExtractRouteSegment(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 3 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            return segments[2];

        if (segments.Length >= 5
            && segments[0].Equals("t", StringComparison.OrdinalIgnoreCase)
            && segments[2].Equals("api", StringComparison.OrdinalIgnoreCase))
            return segments[4];

        return null;
    }
}
