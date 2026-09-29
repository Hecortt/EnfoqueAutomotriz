using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Almacen;
using EnfoqueAutomotriz.Domain.Auth;
using EnfoqueAutomotriz.Domain.Ordenes;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class AlmacenRepository : IAlmacenRepository
{
    private readonly AppDbContext _db;
    private readonly INotificacionRepository _notificaciones;

    private static readonly int[] _rolesNotificables = [1, 6];

    public AlmacenRepository(AppDbContext db, INotificacionRepository notificaciones)
    {
        _db = db;
        _notificaciones = notificaciones;
    }

    public Task<List<Refaccion>> GetAllAsync(string? categoria = null, CancellationToken ct = default)
    {
        var query = _db.Refacciones.AsQueryable();
        if (!string.IsNullOrWhiteSpace(categoria)) query = query.Where(r => r.Categoria == categoria);
        return query.OrderBy(r => r.Nombre).ToListAsync(ct);
    }

    public async Task<List<Refaccion>> GetCriticasAsync(CancellationToken ct = default)
    {
        var todas = await _db.Refacciones.ToListAsync(ct);
        return todas.Where(r => r.Nivel != NivelStock.Normal).OrderBy(r => r.Nivel == NivelStock.Critico ? 0 : 1).ToList();
    }

    public Task<Refaccion?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Refacciones.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task CreateAsync(Refaccion refaccion, CancellationToken ct = default)
    {
        _db.Refacciones.Add(refaccion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Refaccion refaccion, CancellationToken ct = default)
    {
        refaccion.UpdatedAt = DateTime.UtcNow;
        _db.Refacciones.Update(refaccion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AjustarStockAsync(Guid refaccionId, int cantidad, TipoMovimientoInventario tipo, string? motivo, Guid? ordenServicioId, CancellationToken ct = default)
    {
        var refaccion = await _db.Refacciones.FirstOrDefaultAsync(r => r.Id == refaccionId, ct);
        if (refaccion == null) return;

        refaccion.StockActual += tipo == TipoMovimientoInventario.Salida ? -Math.Abs(cantidad) : Math.Abs(cantidad);
        if (refaccion.StockActual < 0) refaccion.StockActual = 0;

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            RefaccionId = refaccionId,
            Tipo = tipo,
            Cantidad = Math.Abs(cantidad),
            Motivo = motivo,
            OrdenServicioId = ordenServicioId
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<SolicitudAlmacen?> CrearSolicitudDesdeOrdenAsync(Guid ordenServicioId, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio
            .Include(o => o.Conceptos)
            .FirstOrDefaultAsync(o => o.Id == ordenServicioId, ct);
        if (orden == null) return null;

        var refaccionesConceptos = orden.Conceptos
            .Where(c => !c.IsDeleted && c.Tipo == TipoConceptoOrden.Refaccion && c.RefaccionId.HasValue)
            .GroupBy(c => c.RefaccionId!.Value)
            .Select(g => new SolicitudAlmacenDetalle { RefaccionId = g.Key, Cantidad = (int)g.Sum(c => c.Cantidad) })
            .ToList();

        if (refaccionesConceptos.Count == 0) return null;

        var solicitud = new SolicitudAlmacen
        {
            OrdenServicioId = ordenServicioId,
            SucursalId = orden.SucursalId,
            Estado = EstadoSolicitudAlmacen.Pendiente,
            Detalles = refaccionesConceptos
        };

        _db.SolicitudesAlmacen.Add(solicitud);
        await _db.SaveChangesAsync(ct);
        return solicitud;
    }

    public Task<List<SolicitudAlmacen>> GetSolicitudesAsync(EstadoSolicitudAlmacen? estado = null, Guid? sucursalId = null, CancellationToken ct = default)
    {
        var query = _db.SolicitudesAlmacen
            .Include(s => s.Detalles).ThenInclude(d => d.Refaccion)
            .Include(s => s.OrdenServicio!).ThenInclude(o => o.Cliente)
            .Include(s => s.OrdenServicio!).ThenInclude(o => o.Vehiculo)
            .Include(s => s.Sucursal)
            .AsQueryable();
        if (estado.HasValue) query = query.Where(s => s.Estado == estado.Value);
        if (sucursalId.HasValue) query = query.Where(s => s.SucursalId == sucursalId.Value);
        return query.OrderByDescending(s => s.FechaSolicitud).ToListAsync(ct);
    }

    public Task<SolicitudAlmacen?> GetSolicitudByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SolicitudesAlmacen
            .Include(s => s.Detalles).ThenInclude(d => d.Refaccion)
            .Include(s => s.OrdenServicio!).ThenInclude(o => o.Cliente)
            .Include(s => s.OrdenServicio!).ThenInclude(o => o.Vehiculo)
            .Include(s => s.Sucursal)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task CambiarEstadoSolicitudAsync(Guid solicitudId, EstadoSolicitudAlmacen estado, CancellationToken ct = default)
    {
        var solicitud = await _db.SolicitudesAlmacen
            .Include(s => s.Detalles)
            .Include(s => s.OrdenServicio)
            .FirstOrDefaultAsync(s => s.Id == solicitudId, ct);
        if (solicitud == null) return;

        solicitud.Estado = estado;
        if (estado == EstadoSolicitudAlmacen.Listo) solicitud.FechaListo = DateTime.UtcNow;

        if (estado == EstadoSolicitudAlmacen.Entregado)
        {
            foreach (var detalle in solicitud.Detalles)
            {
                await AjustarStockAsync(detalle.RefaccionId, detalle.Cantidad, TipoMovimientoInventario.Salida,
                    "Entrega a taller por orden de servicio", solicitud.OrdenServicioId, ct);
            }
        }

        await _db.SaveChangesAsync(ct);

        if (estado == EstadoSolicitudAlmacen.Listo)
        {
            var folio = solicitud.OrdenServicio?.Folio ?? solicitud.OrdenServicioId.ToString();
            await _notificaciones.CreateParaRolesAsync(_rolesNotificables,
                "Refacciones listas para entrega",
                $"Las refacciones de la orden {folio} ya están listas para entregarse al taller.",
                TipoNotificacion.Success,
                $"/Almacen/Solicitudes", ct);
        }
    }
}
