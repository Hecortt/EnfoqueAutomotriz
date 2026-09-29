using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Catalogs;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

/// <summary>Endpoints CRUD para el catálogo genérico multi-propósito (ver Domain.Catalogs.Catalogo).</summary>
public static class CatalogsEndpoints
{
    public static RouteGroupBuilder MapCatalogsEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var catalogs = api.MapGroup("/catalogs").WithTags("Catálogos").RequireAuthorization();

        catalogs.MapGet("/{tipo}", async (string tipo, ICatalogoRepository repo, CancellationToken ct) =>
        {
            var items = await repo.GetByTipoAsync(tipo, ct);
            return Results.Ok(APIResponse<List<Catalogo>>.Success(items));
        })
        .MapToApiVersion(v1)
        .AllowAnonymous()
        .Produces<APIResponse<List<Catalogo>>>();

        catalogs.MapPost("/", async ([FromBody] Catalogo dto, ICatalogoRepository repo, CancellationToken ct) =>
        {
            await repo.CreateAsync(dto, ct);
            return Results.Created($"/api/v1/catalogs/{dto.Tipo}", APIResponse<Catalogo>.Success(dto));
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("AccesoTotal");

        catalogs.MapPut("/{id:guid}", async (Guid id, [FromBody] Catalogo dto, ICatalogoRepository repo, CancellationToken ct) =>
        {
            var existing = await repo.GetByIdAsync(id, ct);
            if (existing == null) return Results.NotFound(APIResponse.Failure("No encontrado"));

            existing.Nombre = dto.Nombre;
            existing.Descripcion = dto.Descripcion;
            existing.Color = dto.Color;
            existing.Icono = dto.Icono;
            existing.Orden = dto.Orden;
            existing.Activo = dto.Activo;

            await repo.UpdateAsync(existing, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("AccesoTotal");

        catalogs.MapDelete("/{id:guid}", async (Guid id, ICatalogoRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1)
        .RequireAuthorization("AccesoTotal");

        return catalogs;
    }
}
