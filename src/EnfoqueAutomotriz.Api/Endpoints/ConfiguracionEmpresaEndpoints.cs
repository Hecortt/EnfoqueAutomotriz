using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Configuracion;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class ConfiguracionEmpresaEndpoints
{
    public static RouteGroupBuilder MapConfiguracionEmpresaEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var cfg = api.MapGroup("/configuracion-empresa").WithTags("Configuración Empresa");

        cfg.MapGet("/", async (IConfiguracionEmpresaRepository repo, CancellationToken ct) =>
        {
            var data = await repo.GetAsync(ct);
            return Results.Ok(APIResponse<ConfiguracionEmpresa?>.Success(data));
        })
        .MapToApiVersion(v1)
        .AllowAnonymous()
        .Produces<APIResponse<ConfiguracionEmpresa?>>();

        cfg.MapPut("/", async ([FromBody] ConfiguracionEmpresa dto, IConfiguracionEmpresaRepository repo, CancellationToken ct) =>
        {
            await repo.UpsertAsync(dto, ct);
            return Results.Ok(APIResponse.Success("Configuración actualizada."));
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("AccesoTotal");

        return cfg;
    }
}
