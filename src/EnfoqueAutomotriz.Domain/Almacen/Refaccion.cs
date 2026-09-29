using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Almacen;

public enum NivelStock { Normal = 1, Reorden = 2, Critico = 3 }

/// <summary>Refacción / insumo del inventario del taller.</summary>
public class Refaccion : CatalogEntity
{
    public string Sku { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string Unidad { get; set; } = "pza";
    public decimal Costo { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public int PuntoReorden { get; set; }
    public Guid? ProveedorId { get; set; }

    /// <summary>Calculado: no se persiste, se deriva de StockActual vs. los umbrales.</summary>
    public NivelStock Nivel => StockActual <= StockMinimo
        ? NivelStock.Critico
        : StockActual <= PuntoReorden
            ? NivelStock.Reorden
            : NivelStock.Normal;
}
