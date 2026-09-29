namespace EnfoqueAutomotriz.Domain.Almacen;

public enum TipoMovimientoInventario { Entrada = 1, Salida = 2, Ajuste = 3 }

public class MovimientoInventario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RefaccionId { get; set; }
    public TipoMovimientoInventario Tipo { get; set; }
    public int Cantidad { get; set; }
    public string? Motivo { get; set; }
    public Guid? OrdenServicioId { get; set; }
    public Guid? OrdenCompraId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public Refaccion? Refaccion { get; set; }
}
