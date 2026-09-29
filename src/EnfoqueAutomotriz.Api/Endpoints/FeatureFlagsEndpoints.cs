using Asp.Versioning;
using EnfoqueAutomotriz.Api.Extensions;
using EnfoqueAutomotriz.Api.Middleware;
using EnfoqueAutomotriz.Domain.Shared;
using Npgsql;

namespace EnfoqueAutomotriz.Api.Endpoints;

/// <summary>Consulta pública (para el tenant autenticado) de sus feature flags activos.</summary>
public static class FeatureFlagsEndpoints
{
    public static RouteGroupBuilder MapFeatureFlagsEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var ff = api.MapGroup("/feature-flags").WithTags("Feature Flags").RequireAuthorization();

        ff.MapGet("/", async (ITenantContext tenant, IConfiguration config, CancellationToken ct) =>
        {
            if (!tenant.IsResolved)
                return Results.Ok(APIResponse<List<string>>.Success([]));

            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ff.feature FROM master.feature_flags ff
                INNER JOIN master.tenants t ON t.id = ff.tenant_id
                WHERE t.slug = $1 AND ff.habilitado = TRUE";
            cmd.Parameters.AddWithValue(tenant.Slug);

            var features = new List<string>();
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) features.Add(r.GetString(0));

            return Results.Ok(APIResponse<List<string>>.Success(features));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<List<string>>>();

        return ff;
    }
}
