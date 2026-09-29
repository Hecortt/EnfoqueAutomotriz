using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Almacen;
using EnfoqueAutomotriz.Domain.Compras;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class ComprasRepository : IComprasRepository
{
    private readonly AppDbContext _db;
    public ComprasRepository(AppDbContext db) => _db = db;

    public Task<List<Proveedor>> GetProveedoresAsync(CancellationToken ct = default) =>
        _db.Proveedores.OrderBy(p => p.Nombre).ToListAsync(ct);

    public Task<Proveedor?> GetProveedorByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task CreateProveedorAsync(Proveedor proveedor, CancellationToken ct = default)
    {
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateProveedorAsync(Proveedor proveedor, CancellationToken ct = default)
    {
        proveedor.UpdatedAt = DateTime.UtcNow;
        _db.Proveedores.Update(proveedor);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<OrdenCompra>> GetOrdenesAsync(EstadoOrdenCompra? estado = null, CancellationToken ct = default)
    {
        var query = _db.OrdenesCompra.Include(o => o.Proveedor).Include(o => o.Detalles).ThenInclude(d => d.Refaccion).AsQueryable();
        if (estado.HasValue) query = query.Where(o => o.Estado == estado.Value);
        return query.OrderByDescending(o => o.FechaOrden).ToListAsync(ct);
    }

    public Task<OrdenCompra?> GetOrdenByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.OrdenesCompra.Include(o => o.Proveedor).Include(o => o.Detalles).ThenInclude(d => d.Refaccion).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<string> GenerarFolioAsync(CancellationToken ct = default)
    {
        var count = await _db.OrdenesCompra.IgnoreQueryFilters().CountAsync(ct);
        return $"OC-{(count + 1):D5}";
    }

    public async Task CreateOrdenAsync(OrdenCompra orden, CancellationToken ct = default)
    {
        orden.Total = orden.Detalles.Sum(d => d.CantidadPedida * d.CostoUnitario);
        _db.OrdenesCompra.Add(orden);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecibirOrdenAsync(Guid ordenId, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesCompra.Include(o => o.Detalles).FirstOrDefaultAsync(o => o.Id == ordenId, ct);
        if (orden == null) return;

        foreach (var detalle in orden.Detalles)
        {
            detalle.CantidadRecibida = detalle.CantidadPedida;

            var refaccion = await _db.Refacciones.FirstOrDefaultAsync(r => r.Id == detalle.RefaccionId, ct);
            if (refaccion != null)
            {
                refaccion.StockActual += detalle.CantidadPedida;
                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    RefaccionId = refaccion.Id,
                    Tipo = TipoMovimientoInventario.Entrada,
                    Cantidad = detalle.CantidadPedida,
                    Motivo = $"Recepción de orden de compra {orden.Folio}",
                    OrdenCompraId = orden.Id
                });
            }
        }

        orden.Estado = EstadoOrdenCompra.RecibidaCompleta;
        orden.FechaRecibida = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
