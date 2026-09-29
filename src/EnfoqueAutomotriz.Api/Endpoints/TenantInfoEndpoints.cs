using Asp.Versioning;
using EnfoqueAutomotriz.Api.Extensions;
using EnfoqueAutomotriz.Api.Middleware;
using EnfoqueAutomotriz.Domain.Shared;
using Npgsql;

namespace EnfoqueAutomotriz.Api.Endpoints;

/// <summary>Información básica del tenant actual (slug, plan, nombre) para mostrar en la UI (ej. badge de plan en el sidebar).</summary>
public static class TenantInfoEndpoints
{
    public static RouteGroupBuilder MapTenantInfoEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/tenant-info").WithTags("Tenant Info").RequireAuthorization();

        g.MapGet("/", async (ITenantContext tenant, IConfiguration config, CancellationToken ct) =>
        {
            if (!tenant.IsResolved)
                return Results.Ok(APIResponse<object>.Success(new { slug = "", plan = "", nombreComercial = "" }));

            var connStr = DatabaseExtensions.BuildConnectionString(config);
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT slug, plan, nombre_comercial FROM master.tenants WHERE slug = $1";
            cmd.Parameters.AddWithValue(tenant.Slug);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct))
                return Results.Ok(APIResponse<object>.Success(new { slug = tenant.Slug, plan = "", nombreComercial = "" }));

            return Results.Ok(APIResponse<object>.Success(new
            {
                slug = r.GetString(0),
                plan = r.GetString(1),
                nombreComercial = r.GetString(2)
            }));
        }).MapToApiVersion(v1);

        return g;
    }
}
