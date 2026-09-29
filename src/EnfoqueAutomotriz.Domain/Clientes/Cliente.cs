using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Clientes;

public class Cliente : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Rfc { get; set; }
    public string? Direccion { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;

    // Campos cacheados para no recalcular en cada consulta (se actualizan al crear una orden)
    public int NumeroVisitas { get; set; }
    public DateTime? PrimeraVisita { get; set; }
    public DateTime? UltimaVisita { get; set; }

    public ICollection<Vehiculo> Vehiculos { get; set; } = [];
}
