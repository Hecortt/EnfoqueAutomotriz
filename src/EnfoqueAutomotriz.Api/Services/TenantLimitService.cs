using EnfoqueAutomotriz.Api.Extensions;
using EnfoqueAutomotriz.Api.Middleware;
using Npgsql;

namespace EnfoqueAutomotriz.Api.Services;

public interface ITenantLimitService
{
    Task<(bool allowed, string? error)> CheckUsuarioLimitAsync(CancellationToken ct = default);
}

/// <summary>
/// Verifica cuotas del plan contratado por el tenant (taller): usuarios, etc.
/// Se ampliará con más límites conforme se agreguen módulos de negocio
/// (ej. max_ordenes_activas, max_sucursales).
/// </summary>
public sealed class TenantLimitService : ITenantLimitService
{
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _config;
    private readonly ILogger<TenantLimitService> _logger;

    public TenantLimitService(ITenantContext tenant, IConfiguration config, ILogger<TenantLimitService> logger)
    {
        _tenant = tenant;
        _config = config;
        _logger = logger;
    }

    public Task<(bool allowed, string? error)> CheckUsuarioLimitAsync(CancellationToken ct)
        => CheckLimitAsync("usuarios", "max_usuarios", "usuarios", ct);

    private async Task<(bool allowed, string? error)> CheckLimitAsync(
        string tableName, string limitColumn, string entityLabel, CancellationToken ct)
    {
        if (!_tenant.IsResolved) return (true, null);

        try
        {
            var connStr = DatabaseExtensions.BuildConnectionString(_config);
            if (string.IsNullOrEmpty(connStr)) return (true, null);

            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT
                    COALESCE(t.{limitColumn}, p.{limitColumn}, -1) AS max_allowed,
                    (SELECT COUNT(1) FROM ""{_tenant.SchemaName}"".{tableName} WHERE NOT is_deleted) AS current_count
                FROM master.tenants t
                LEFT JOIN master.planes p ON p.slug = t.plan
                WHERE t.slug = $1";
            cmd.Parameters.AddWithValue(_tenant.Slug ?? "");

            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return (true, null);

            var maxAllowed = r.GetInt32(0);
            var currentCount = r.GetInt64(1);

            if (maxAllowed < 0) return (true, null); // -1 = ilimitado

            if (currentCount >= maxAllowed)
            {
                _logger.LogWarning(
                    "Límite de {Entity} alcanzado para tenant '{Tenant}': {Current}/{Max}",
                    entityLabel, _tenant.Slug, currentCount, maxAllowed);
                return (false, $"Límite de {entityLabel} alcanzado ({currentCount}/{maxAllowed}). Actualice su plan para agregar más.");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando límite de {Entity} para tenant '{Tenant}'", entityLabel, _tenant.Slug);
            return (true, null); // Fail open
        }
    }
}
