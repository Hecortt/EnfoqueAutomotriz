using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.RH;

/// <summary>Catálogo de cursos/capacitaciones disponibles para el personal del taller.</summary>
public class Capacitacion : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Categoria { get; set; }
    public int DuracionHoras { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<EmpleadoCapacitacion> Asignaciones { get; set; } = [];
}

public enum EstadoCapacitacionEmpleado
{
    Pendiente = 1,
    EnProgreso = 2,
    Completada = 3
}

/// <summary>Asignación de una capacitación a un empleado y su progreso/resultado.</summary>
public class EmpleadoCapacitacion : BaseEntity
{
    public Guid EmpleadoId { get; set; }
    public Guid CapacitacionId { get; set; }
    public EstadoCapacitacionEmpleado Estado { get; set; } = EstadoCapacitacionEmpleado.Pendiente;
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCompletado { get; set; }
    public decimal? CalificacionFinal { get; set; }

    public Empleado? Empleado { get; set; }
    public Capacitacion? Capacitacion { get; set; }
}
