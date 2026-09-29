using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Compras;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class ComprasEndpoints
{
    public static RouteGroupBuilder MapComprasEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/compras").WithTags("Compras").RequireAuthorization();

        g.MapGet("/proveedores", async (IComprasRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Proveedor>>.Success(await repo.GetProveedoresAsync(ct))))
            .MapToApiVersion(v1);

        g.MapPost("/proveedores", async ([FromBody] Proveedor dto, IComprasRepository repo, CancellationToken ct) =>
        {
            await repo.CreateProveedorAsync(dto, ct);
            return Results.Created($"/api/v1/compras/proveedores/{dto.Id}", APIResponse<Proveedor>.Success(dto));
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapGet("/ordenes", async ([FromQuery] EstadoOrdenCompra? estado, IComprasRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<OrdenCompra>>.Success(await repo.GetOrdenesAsync(estado, ct))))
            .MapToApiVersion(v1);

        g.MapGet("/ordenes/{id:guid}", async (Guid id, IComprasRepository repo, CancellationToken ct) =>
        {
            var o = await repo.GetOrdenByIdAsync(id, ct);
            return o == null ? Results.NotFound(APIResponse.Failure("No encontrada")) : Results.Ok(APIResponse<OrdenCompra>.Success(o));
        }).MapToApiVersion(v1);

        g.MapPost("/ordenes", async ([FromBody] OrdenCompraCreateDto dto, IComprasRepository repo, CancellationToken ct) =>
        {
            var folio = await repo.GenerarFolioAsync(ct);
            var orden = new OrdenCompra
            {
                Folio = folio,
                ProveedorId = dto.ProveedorId,
                SucursalId = dto.SucursalId,
                FechaEsperada = dto.FechaEsperada,
                Estado = EstadoOrdenCompra.Enviada,
                Detalles = dto.Detalles.Select(d => new OrdenCompraDetalle
                {
                    RefaccionId = d.RefaccionId,
                    CantidadPedida = d.Cantidad,
                    CostoUnitario = d.CostoUnitario
                }).ToList()
            };
            await repo.CreateOrdenAsync(orden, ct);
            return Results.Created($"/api/v1/compras/ordenes/{orden.Id}", APIResponse<OrdenCompra>.Success(orden));
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapPatch("/ordenes/{id:guid}/recibir", async (Guid id, IComprasRepository repo, CancellationToken ct) =>
        {
            await repo.RecibirOrdenAsync(id, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        return g;
    }
}

public record OrdenCompraCreateDto(Guid ProveedorId, Guid SucursalId, DateTime? FechaEsperada, List<OrdenCompraDetalleDto> Detalles);
public record OrdenCompraDetalleDto(Guid RefaccionId, int Cantidad, decimal CostoUnitario);
