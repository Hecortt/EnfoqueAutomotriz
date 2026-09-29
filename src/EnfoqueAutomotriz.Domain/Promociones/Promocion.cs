using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Promociones;

public enum TipoDescuentoPromocion { Porcentaje = 1, MontoFijo = 2 }
public enum AplicaPromocionA { Todos = 1, ClientesRecurrentes = 2, PrimeraVisita = 3 }

public class Promocion : CatalogEntity
{
    public string? Descripcion { get; set; }
    public TipoDescuentoPromocion TipoDescuento { get; set; } = TipoDescuentoPromocion.Porcentaje;
    public decimal Valor { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime FechaFin { get; set; }
    public AplicaPromocionA AplicaA { get; set; } = AplicaPromocionA.Todos;

    public bool Vigente => Activo && DateTime.UtcNow >= FechaInicio && DateTime.UtcNow <= FechaFin;
}
