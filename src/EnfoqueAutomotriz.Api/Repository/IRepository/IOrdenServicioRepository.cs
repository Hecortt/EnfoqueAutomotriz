using EnfoqueAutomotriz.Domain.Ordenes;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IOrdenServicioRepository
{
    Task<List<OrdenServicio>> GetAllAsync(Guid? sucursalId = null, EstadoOrdenServicio? estado = null, CancellationToken ct = default);
    Task<OrdenServicio?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OrdenServicio?> GetByTokenAsync(Guid token, CancellationToken ct = default);
    Task<string> GenerarFolioAsync(CancellationToken ct = default);
    Task CreateAsync(OrdenServicio orden, CancellationToken ct = default);
    Task UpdateAsync(OrdenServicio orden, CancellationToken ct = default);

    Task AddConceptoAsync(OrdenServicioConcepto concepto, CancellationToken ct = default);
    Task RemoveConceptoAsync(Guid conceptoId, CancellationToken ct = default);
    Task RecalcularTotalesAsync(Guid ordenId, CancellationToken ct = default);

    Task AddFotoAsync(OrdenServicioFoto foto, CancellationToken ct = default);

    Task<OrdenServicioMarcaDano> AddMarcaDanoAsync(OrdenServicioMarcaDano marca, CancellationToken ct = default);
    Task RemoveMarcaDanoAsync(Guid marcaId, CancellationToken ct = default);
    Task SetFechaEntregaProgramadaAsync(Guid ordenId, DateTime? fecha, CancellationToken ct = default);
    Task UpdateProblemaReportadoAsync(Guid ordenId, string problemaReportado, CancellationToken ct = default);

    Task<List<object>> GetMecanicosAsync(Guid ordenId, CancellationToken ct = default);
    Task<object> AddMecanicoAsync(Guid ordenId, Guid empleadoId, CancellationToken ct = default);
    Task RemoveMecanicoAsync(Guid asignacionId, CancellationToken ct = default);
    Task<List<object>> GetMecanicosDisponiblesAsync(Guid? sucursalId = null, CancellationToken ct = default);

    Task CambiarEstadoAsync(Guid ordenId, EstadoOrdenServicio nuevoEstado, string? comentario, Guid? usuarioId, CancellationToken ct = default);
    Task EnviarCotizacionAsync(Guid ordenId, CancellationToken ct = default);
    Task<bool> AprobarCotizacionAsync(Guid token, CancellationToken ct = default);
    Task<bool> RechazarCotizacionAsync(Guid token, string? comentario, CancellationToken ct = default);

    Task RegistrarEncuestaAsync(Guid ordenId, int calificacion, string? comentario, CancellationToken ct = default);

    Task<object> GetDashboardKpisAsync(Guid? sucursalId = null, CancellationToken ct = default);
    Task<object> GetDashboardEmpresaAsync(CancellationToken ct = default);
    Task<object?> GetDashboardSucursalAsync(Guid sucursalId, CancellationToken ct = default);
    Task<object> GetDashboardCalidadAsync(Guid? sucursalId = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default);
    Task<object?> GetDesempenoTecnicoAsync(Guid empleadoId, CancellationToken ct = default);

    /// <summary>Ficha de "Portal de Entrega": todo lo necesario para presentarle al cliente, en sala,
    /// el resumen de su servicio y su historial de visitas al momento de recoger el vehículo.</summary>
    Task<object?> GetPortalEntregaAsync(Guid ordenId, CancellationToken ct = default);

    /// <summary>Eventos de agenda (recepción, entrega programada y entrega real) de todas las
    /// órdenes de servicio que caen dentro del rango de fechas indicado, para alimentar el
    /// calendario general del taller.</summary>
    Task<List<object>> GetAgendaAsync(DateTime desde, DateTime hasta, Guid? sucursalId = null, EstadoOrdenServicio? estado = null, CancellationToken ct = default);

    /// <summary>Resumen de métricas reales (histórico) para la presentación ejecutiva: dinero
    /// gestionado en la plataforma, cumplimiento de entregas y satisfacción real del cliente.</summary>
    Task<object> GetResumenPresentacionAsync(CancellationToken ct = default);
}
