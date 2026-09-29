using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Almacen;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class AlmacenEndpoints
{
    public static RouteGroupBuilder MapAlmacenEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/almacen").WithTags("Almacén").RequireAuthorization();

        g.MapGet("/refacciones", async ([FromQuery] string? categoria, IAlmacenRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Refaccion>>.Success(await repo.GetAllAsync(categoria, ct))))
            .MapToApiVersion(v1);

        g.MapGet("/refacciones/criticas", async (IAlmacenRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Refaccion>>.Success(await repo.GetCriticasAsync(ct))))
            .MapToApiVersion(v1);

        g.MapGet("/refacciones/{id:guid}", async (Guid id, IAlmacenRepository repo, CancellationToken ct) =>
        {
            var r = await repo.GetByIdAsync(id, ct);
            return r == null ? Results.NotFound(APIResponse.Failure("No encontrada")) : Results.Ok(APIResponse<Refaccion>.Success(r));
        }).MapToApiVersion(v1);

        g.MapPost("/refacciones", async ([FromBody] Refaccion dto, IAlmacenRepository repo, CancellationToken ct) =>
        {
            await repo.CreateAsync(dto, ct);
            return Results.Created($"/api/v1/almacen/refacciones/{dto.Id}", APIResponse<Refaccion>.Success(dto));
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapPut("/refacciones/{id:guid}", async (Guid id, [FromBody] Refaccion dto, IAlmacenRepository repo, CancellationToken ct) =>
        {
            var r = await repo.GetByIdAsync(id, ct);
            if (r == null) return Results.NotFound(APIResponse.Failure("No encontrada"));
            r.Nombre = dto.Nombre; r.Sku = dto.Sku; r.Categoria = dto.Categoria; r.Unidad = dto.Unidad;
            r.Costo = dto.Costo; r.PrecioVenta = dto.PrecioVenta; r.StockMinimo = dto.StockMinimo;
            r.PuntoReorden = dto.PuntoReorden; r.ProveedorId = dto.ProveedorId; r.Activo = dto.Activo;
            await repo.UpdateAsync(r, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapPatch("/refacciones/{id:guid}/ajuste", async (Guid id, [FromBody] AjusteStockDto dto, IAlmacenRepository repo, CancellationToken ct) =>
        {
            await repo.AjustarStockAsync(id, dto.Cantidad, dto.Tipo, dto.Motivo, null, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        // ── Solicitudes de almacén (generadas al aprobar cotización) ────────────
        g.MapGet("/solicitudes", async ([FromQuery] EstadoSolicitudAlmacen? estado, [FromQuery] Guid? sucursalId, IAlmacenRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<SolicitudAlmacen>>.Success(await repo.GetSolicitudesAsync(estado, sucursalId, ct))))
            .MapToApiVersion(v1);

        g.MapGet("/solicitudes/{id:guid}", async (Guid id, IAlmacenRepository repo, CancellationToken ct) =>
        {
            var s = await repo.GetSolicitudByIdAsync(id, ct);
            return s == null ? Results.NotFound(APIResponse.Failure("No encontrada")) : Results.Ok(APIResponse<SolicitudAlmacen>.Success(s));
        }).MapToApiVersion(v1);

        g.MapPatch("/solicitudes/{id:guid}/estado", async (Guid id, [FromBody] EstadoSolicitudDto dto, IAlmacenRepository repo, CancellationToken ct) =>
        {
            await repo.CambiarEstadoSolicitudAsync(id, dto.Estado, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        return g;
    }
}

public record AjusteStockDto(int Cantidad, EnfoqueAutomotriz.Domain.Almacen.TipoMovimientoInventario Tipo, string? Motivo);
public record EstadoSolicitudDto(EstadoSolicitudAlmacen Estado);
