using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Shared;
using EnfoqueAutomotriz.Domain.Sucursales;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class SucursalesEndpoints
{
    public static RouteGroupBuilder MapSucursalesEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/sucursales").WithTags("Sucursales").RequireAuthorization();

        g.MapGet("/", async (ISucursalRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Sucursal>>.Success(await repo.GetAllAsync(ct))))
            .MapToApiVersion(v1);

        g.MapGet("/{id:guid}", async (Guid id, ISucursalRepository repo, CancellationToken ct) =>
        {
            var s = await repo.GetByIdAsync(id, ct);
            return s == null ? Results.NotFound(APIResponse.Failure("No encontrada")) : Results.Ok(APIResponse<Sucursal>.Success(s));
        }).MapToApiVersion(v1);

        g.MapPost("/", async ([FromBody] Sucursal dto, ISucursalRepository repo, CancellationToken ct) =>
        {
            await repo.CreateAsync(dto, ct);
            return Results.Created($"/api/v1/sucursales/{dto.Id}", APIResponse<Sucursal>.Success(dto));
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapPut("/{id:guid}", async (Guid id, [FromBody] Sucursal dto, ISucursalRepository repo, CancellationToken ct) =>
        {
            var s = await repo.GetByIdAsync(id, ct);
            if (s == null) return Results.NotFound(APIResponse.Failure("No encontrada"));
            s.Nombre = dto.Nombre; s.Direccion = dto.Direccion; s.Telefono = dto.Telefono; s.Activa = dto.Activa;
            await repo.UpdateAsync(s, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapDelete("/{id:guid}", async (Guid id, ISucursalRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        return g;
    }
}
