using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Auth;
using EnfoqueAutomotriz.Domain.RH;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly AppDbContext _db;
    private readonly INotificacionRepository _notificaciones;

    private static readonly int[] _rolesNotificables = [1, 6];

    public EmpleadoRepository(AppDbContext db, INotificacionRepository notificaciones)
    {
        _db = db;
        _notificaciones = notificaciones;
    }

    public Task<List<Empleado>> GetAllAsync(CancellationToken ct = default) =>
        _db.Empleados.Include(e => e.Puesto).ThenInclude(p => p!.Departamento).Include(e => e.Sucursal)
            .OrderBy(e => e.Nombres).ToListAsync(ct);

    public Task<Empleado?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Empleados.Include(e => e.Puesto).ThenInclude(p => p!.Departamento).Include(e => e.Sucursal)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<string> GenerarCodigoAsync(CancellationToken ct = default)
    {
        var count = await _db.Empleados.IgnoreQueryFilters().CountAsync(ct);
        return $"EMP-{(count + 1):D4}";
    }

    public async Task CreateAsync(Empleado empleado, CancellationToken ct = default)
    {
        _db.Empleados.Add(empleado);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Empleado empleado, CancellationToken ct = default)
    {
        empleado.UpdatedAt = DateTime.UtcNow;
        _db.Empleados.Update(empleado);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<OrganigramaNodo>> GetOrganigramaAsync(CancellationToken ct = default)
    {
        var empleados = await _db.Empleados
            .Include(e => e.Puesto).ThenInclude(p => p!.Departamento)
            .Where(e => e.Activo)
            .ToListAsync(ct);

        var nodos = empleados.ToDictionary(e => e.Id, e => new OrganigramaNodo
        {
            Id = e.Id,
            NombreCompleto = e.NombreCompleto,
            Puesto = e.Puesto?.Nombre,
            Departamento = e.Puesto?.Departamento?.Nombre,
            FotoUrl = e.FotoUrl
        });

        var raices = new List<OrganigramaNodo>();
        foreach (var e in empleados)
        {
            var nodo = nodos[e.Id];
            if (e.JefeDirectoId.HasValue && nodos.TryGetValue(e.JefeDirectoId.Value, out var jefe))
                jefe.Subordinados.Add(nodo);
            else
                raices.Add(nodo);
        }

        return raices;
    }

    public Task<List<Departamento>> GetDepartamentosAsync(CancellationToken ct = default) =>
        _db.Departamentos.OrderBy(d => d.Orden).ThenBy(d => d.Nombre).ToListAsync(ct);

    public Task<List<Puesto>> GetPuestosAsync(Guid? departamentoId = null, CancellationToken ct = default)
    {
        var query = _db.Puestos.Include(p => p.Departamento).AsQueryable();
        if (departamentoId.HasValue) query = query.Where(p => p.DepartamentoId == departamentoId.Value);
        return query.OrderBy(p => p.Nivel).ThenBy(p => p.Nombre).ToListAsync(ct);
    }

    public async Task CreateDepartamentoAsync(Departamento departamento, CancellationToken ct = default)
    {
        _db.Departamentos.Add(departamento);
        await _db.SaveChangesAsync(ct);
    }

    public async Task CreatePuestoAsync(Puesto puesto, CancellationToken ct = default)
    {
        _db.Puestos.Add(puesto);
        await _db.SaveChangesAsync(ct);
    }

    // ── Capacitación ───────────────────────────────────────────────────────

    public Task<List<Capacitacion>> GetCapacitacionesAsync(CancellationToken ct = default) =>
        _db.Capacitaciones.Where(c => c.Activo).OrderBy(c => c.Categoria).ThenBy(c => c.Titulo).ToListAsync(ct);

    public async Task CreateCapacitacionAsync(Capacitacion capacitacion, CancellationToken ct = default)
    {
        _db.Capacitaciones.Add(capacitacion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateCapacitacionAsync(Capacitacion capacitacion, CancellationToken ct = default)
    {
        capacitacion.UpdatedAt = DateTime.UtcNow;
        _db.Capacitaciones.Update(capacitacion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<EmpleadoCapacitacion>> GetAsignacionesAsync(Guid? empleadoId = null, Guid? capacitacionId = null, CancellationToken ct = default)
    {
        var query = _db.EmpleadosCapacitaciones
            .Include(a => a.Capacitacion)
            .Include(a => a.Empleado)
            .AsQueryable();

        if (empleadoId.HasValue) query = query.Where(a => a.EmpleadoId == empleadoId.Value);
        if (capacitacionId.HasValue) query = query.Where(a => a.CapacitacionId == capacitacionId.Value);

        return await query.OrderByDescending(a => a.FechaAsignacion).ToListAsync(ct);
    }

    public async Task<EmpleadoCapacitacion> AsignarCapacitacionAsync(Guid empleadoId, Guid capacitacionId, CancellationToken ct = default)
    {
        var yaAsignado = await _db.EmpleadosCapacitaciones.AnyAsync(a => a.EmpleadoId == empleadoId && a.CapacitacionId == capacitacionId, ct);
        if (yaAsignado)
            throw new InvalidOperationException("Este curso ya está asignado a este empleado.");

        var asignacion = new EmpleadoCapacitacion { EmpleadoId = empleadoId, CapacitacionId = capacitacionId };
        _db.EmpleadosCapacitaciones.Add(asignacion);
        await _db.SaveChangesAsync(ct);
        return asignacion;
    }

    public async Task ActualizarEstadoAsignacionAsync(Guid asignacionId, EstadoCapacitacionEmpleado estado, decimal? calificacionFinal, CancellationToken ct = default)
    {
        var asignacion = await _db.EmpleadosCapacitaciones
            .Include(a => a.Empleado)
            .Include(a => a.Capacitacion)
            .FirstOrDefaultAsync(a => a.Id == asignacionId, ct);
        if (asignacion == null) return;

        asignacion.Estado = estado;
        asignacion.UpdatedAt = DateTime.UtcNow;
        if (estado == EstadoCapacitacionEmpleado.Completada)
        {
            asignacion.FechaCompletado = DateTime.UtcNow;
            asignacion.CalificacionFinal = calificacionFinal;
        }
        else
        {
            asignacion.FechaCompletado = null;
            asignacion.CalificacionFinal = null;
        }

        await _db.SaveChangesAsync(ct);

        if (estado == EstadoCapacitacionEmpleado.Completada)
        {
            var nombreEmpleado = asignacion.Empleado?.NombreCompleto ?? "Un empleado";
            var tituloCurso = asignacion.Capacitacion?.Titulo ?? "un curso";
            await _notificaciones.CreateParaRolesAsync(_rolesNotificables,
                "Capacitación completada",
                $"{nombreEmpleado} completó el curso '{tituloCurso}'" + (calificacionFinal.HasValue ? $" con calificación {calificacionFinal}%." : "."),
                TipoNotificacion.Success,
                "/RH/Capacitacion/Index", ct);
        }
    }

    public async Task EliminarAsignacionAsync(Guid asignacionId, CancellationToken ct = default)
    {
        var asignacion = await _db.EmpleadosCapacitaciones.FirstOrDefaultAsync(a => a.Id == asignacionId, ct);
        if (asignacion == null) return;
        asignacion.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }
}
