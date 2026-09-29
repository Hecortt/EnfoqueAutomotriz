using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Compras;

public enum EstadoOrdenCompra { Borrador = 1, Enviada = 2, RecibidaParcial = 3, RecibidaCompleta = 4, Cancelada = 5 }

public class OrdenCompra : BaseEntity
{
    public string Folio { get; set; } = string.Empty;
    public Guid ProveedorId { get; set; }
    public Guid SucursalId { get; set; }
    public EstadoOrdenCompra Estado { get; set; } = EstadoOrdenCompra.Borrador;
    public DateTime FechaOrden { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEsperada { get; set; }
    public DateTime? FechaRecibida { get; set; }
    public decimal Total { get; set; }
    public string? Observaciones { get; set; }

    public Proveedor? Proveedor { get; set; }
    public ICollection<OrdenCompraDetalle> Detalles { get; set; } = [];
}

public class OrdenCompraDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenCompraId { get; set; }
    public Guid RefaccionId { get; set; }
    public int CantidadPedida { get; set; }
    public int CantidadRecibida { get; set; }
    public decimal CostoUnitario { get; set; }

    public Almacen.Refaccion? Refaccion { get; set; }
}
