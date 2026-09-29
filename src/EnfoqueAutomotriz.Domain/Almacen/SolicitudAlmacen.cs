using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.Domain.Shared;
using EnfoqueAutomotriz.Domain.Sucursales;

namespace EnfoqueAutomotriz.Domain.Almacen;

public enum EstadoSolicitudAlmacen { Pendiente = 1, Preparando = 2, Listo = 3, Entregado = 4, Cancelado = 5 }

/// <summary>
/// Se genera automáticamente cuando el cliente aprueba la cotización de una
/// orden de servicio: alerta a almacén para que prepare las refacciones necesarias.
/// </summary>
public class SolicitudAlmacen : BaseEntity
{
    public Guid OrdenServicioId { get; set; }
    public Guid SucursalId { get; set; }
    public EstadoSolicitudAlmacen Estado { get; set; } = EstadoSolicitudAlmacen.Pendiente;
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaListo { get; set; }
    public string? Observaciones { get; set; }

    public OrdenServicio? OrdenServicio { get; set; }
    public Sucursal? Sucursal { get; set; }
    public ICollection<SolicitudAlmacenDetalle> Detalles { get; set; } = [];
}

public class SolicitudAlmacenDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SolicitudAlmacenId { get; set; }
    public Guid RefaccionId { get; set; }
    public int Cantidad { get; set; }

    public Refaccion? Refaccion { get; set; }
}
