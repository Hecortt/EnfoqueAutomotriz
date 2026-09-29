using EnfoqueAutomotriz.Domain.Shared;
using EnfoqueAutomotriz.Domain.Sucursales;

namespace EnfoqueAutomotriz.Domain.RH;

public class Empleado : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string? ApellidoMaterno { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
    public DateTime? FechaBaja { get; set; }
    public decimal SalarioDiario { get; set; }
    public Guid? SucursalId { get; set; }
    public Guid PuestoId { get; set; }
    public Guid? JefeDirectoId { get; set; }
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; } = true;

    public string NombreCompleto => $"{Nombres} {ApellidoPaterno} {ApellidoMaterno}".Trim();

    public Sucursal? Sucursal { get; set; }
    public Puesto? Puesto { get; set; }
    public Empleado? JefeDirecto { get; set; }
    public ICollection<Empleado> Subordinados { get; set; } = [];
}
