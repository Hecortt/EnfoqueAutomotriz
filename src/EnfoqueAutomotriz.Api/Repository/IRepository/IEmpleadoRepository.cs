using EnfoqueAutomotriz.Domain.RH;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IEmpleadoRepository
{
    Task<List<Empleado>> GetAllAsync(CancellationToken ct = default);
    Task<Empleado?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<string> GenerarCodigoAsync(CancellationToken ct = default);
    Task CreateAsync(Empleado empleado, CancellationToken ct = default);
    Task UpdateAsync(Empleado empleado, CancellationToken ct = default);
    Task<List<OrganigramaNodo>> GetOrganigramaAsync(CancellationToken ct = default);

    Task<List<Departamento>> GetDepartamentosAsync(CancellationToken ct = default);
    Task<List<Puesto>> GetPuestosAsync(Guid? departamentoId = null, CancellationToken ct = default);
    Task CreateDepartamentoAsync(Departamento departamento, CancellationToken ct = default);
    Task CreatePuestoAsync(Puesto puesto, CancellationToken ct = default);

    // ── Capacitación ───────────────────────────────────────────────────────
    Task<List<Capacitacion>> GetCapacitacionesAsync(CancellationToken ct = default);
    Task CreateCapacitacionAsync(Capacitacion capacitacion, CancellationToken ct = default);
    Task UpdateCapacitacionAsync(Capacitacion capacitacion, CancellationToken ct = default);

    Task<List<EmpleadoCapacitacion>> GetAsignacionesAsync(Guid? empleadoId = null, Guid? capacitacionId = null, CancellationToken ct = default);
    Task<EmpleadoCapacitacion> AsignarCapacitacionAsync(Guid empleadoId, Guid capacitacionId, CancellationToken ct = default);
    Task ActualizarEstadoAsignacionAsync(Guid asignacionId, EstadoCapacitacionEmpleado estado, decimal? calificacionFinal, CancellationToken ct = default);
    Task EliminarAsignacionAsync(Guid asignacionId, CancellationToken ct = default);
}
