using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Clientes;

public class Vehiculo : BaseEntity
{
    public Guid ClienteId { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string Placas { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Vin { get; set; }
    public int Kilometraje { get; set; }
    public bool Activo { get; set; } = true;

    public Cliente? Cliente { get; set; }
}
