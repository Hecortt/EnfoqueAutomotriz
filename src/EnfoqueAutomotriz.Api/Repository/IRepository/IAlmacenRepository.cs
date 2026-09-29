using EnfoqueAutomotriz.Domain.Almacen;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IAlmacenRepository
{
    Task<List<Refaccion>> GetAllAsync(string? categoria = null, CancellationToken ct = default);
    Task<List<Refaccion>> GetCriticasAsync(CancellationToken ct = default);
    Task<Refaccion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task CreateAsync(Refaccion refaccion, CancellationToken ct = default);
    Task UpdateAsync(Refaccion refaccion, CancellationToken ct = default);
    Task AjustarStockAsync(Guid refaccionId, int cantidad, TipoMovimientoInventario tipo, string? motivo, Guid? ordenServicioId, CancellationToken ct = default);

    Task<SolicitudAlmacen?> CrearSolicitudDesdeOrdenAsync(Guid ordenServicioId, CancellationToken ct = default);
    Task<List<SolicitudAlmacen>> GetSolicitudesAsync(EstadoSolicitudAlmacen? estado = null, Guid? sucursalId = null, CancellationToken ct = default);
    Task<SolicitudAlmacen?> GetSolicitudByIdAsync(Guid id, CancellationToken ct = default);
    Task CambiarEstadoSolicitudAsync(Guid solicitudId, EstadoSolicitudAlmacen estado, CancellationToken ct = default);
}
