using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Promociones;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class PromocionesEndpoints
{
    public static RouteGroupBuilder MapPromocionesEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/promociones").WithTags("Promociones").RequireAuthorization();

        g.MapGet("/", async ([FromQuery] bool? vigentes, IPromocionRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Promocion>>.Success(await repo.GetAllAsync(vigentes ?? false, ct))))
            .MapToApiVersion(v1);

        g.MapPost("/", async ([FromBody] Promocion dto, IPromocionRepository repo, CancellationToken ct) =>
        {
            await repo.CreateAsync(dto, ct);
            return Results.Created($"/api/v1/promociones/{dto.Id}", APIResponse<Promocion>.Success(dto));
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapPut("/{id:guid}", async (Guid id, [FromBody] Promocion dto, IPromocionRepository repo, CancellationToken ct) =>
        {
            var p = await repo.GetByIdAsync(id, ct);
            if (p == null) return Results.NotFound(APIResponse.Failure("No encontrada"));
            p.Nombre = dto.Nombre; p.Descripcion = dto.Descripcion; p.TipoDescuento = dto.TipoDescuento;
            p.Valor = dto.Valor; p.FechaInicio = dto.FechaInicio; p.FechaFin = dto.FechaFin;
            p.AplicaA = dto.AplicaA; p.Activo = dto.Activo;
            await repo.UpdateAsync(p, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapDelete("/{id:guid}", async (Guid id, IPromocionRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        return g;
    }
}
