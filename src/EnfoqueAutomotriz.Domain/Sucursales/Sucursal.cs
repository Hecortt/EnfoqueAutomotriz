using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Sucursales;

public class Sucursal : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public bool Activa { get; set; } = true;
}
