using EnfoqueAutomotriz.Domain.Clientes;
using EnfoqueAutomotriz.Domain.Shared;
using EnfoqueAutomotriz.Domain.Sucursales;

namespace EnfoqueAutomotriz.Domain.Ordenes;

public enum EstadoOrdenServicio
{
    Recibido = 1,
    Diagnostico = 2,
    EsperandoAprobacion = 3,
    EnReparacion = 4,
    Listo = 5,
    Entregado = 6,
    Cancelado = 7
}

public enum EstadoCotizacion
{
    NoAplica = 0,
    Pendiente = 1,
    Aprobada = 2,
    Rechazada = 3
}

public enum TipoConceptoOrden
{
    ManoObra = 1,
    Refaccion = 2
}

/// <summary>
/// Orden de servicio: entidad central del taller. Cubre desde la recepción del
/// vehículo hasta la entrega, con línea de tiempo de estatus, fotos de evidencia
/// y seguimiento público (sin login) vía <see cref="TokenSeguimiento"/>.
/// </summary>
public class OrdenServicio : BaseEntity
{
    public string Folio { get; set; } = string.Empty;
    public Guid SucursalId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid VehiculoId { get; set; }
    public Guid? EmpleadoAsignadoId { get; set; }

    public string ProblemaReportado { get; set; } = string.Empty;
    public int KilometrajeRecepcion { get; set; }
    public string? NivelGasolina { get; set; }
    public string? Observaciones { get; set; }

    public EstadoOrdenServicio Estado { get; set; } = EstadoOrdenServicio.Recibido;

    public EstadoCotizacion EstadoCotizacion { get; set; } = EstadoCotizacion.NoAplica;
    public DateTime? FechaAprobacionCotizacion { get; set; }
    public string? ComentarioRechazo { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public Guid? PromocionId { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public decimal Anticipo { get; set; }
    public decimal Saldo { get; set; }
    public decimal CostoRefacciones { get; set; }

    /// <summary>Token único no adivinable para acceso público (QR / link de seguimiento) sin cuenta.</summary>
    public Guid TokenSeguimiento { get; set; } = Guid.NewGuid();

    public DateTime FechaRecepcion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha/hora comprometida con el cliente para la entrega (se define al recibir o cotizar).</summary>
    public DateTime? FechaEntregaProgramada { get; set; }

    /// <summary>Fecha/hora real en la que el vehículo fue entregado al cliente.</summary>
    public DateTime? FechaEntrega { get; set; }

    /// <summary>Calculado: null si aún no se entrega o no había fecha programada; true si se entregó a tiempo.</summary>
    public bool? EntregadoATiempo => FechaEntrega.HasValue && FechaEntregaProgramada.HasValue
        ? FechaEntrega.Value <= FechaEntregaProgramada.Value
        : null;

    public Sucursal? Sucursal { get; set; }
    public Cliente? Cliente { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    public ICollection<OrdenServicioConcepto> Conceptos { get; set; } = [];
    public ICollection<OrdenServicioFoto> Fotos { get; set; } = [];
    public ICollection<OrdenServicioEstatusHistorial> Historial { get; set; } = [];
    public ICollection<OrdenServicioMarcaDano> MarcasDano { get; set; } = [];
    public ICollection<OrdenServicioMecanico> Mecanicos { get; set; } = [];
}

/// <summary>Línea de mano de obra o refacción cotizada/usada en la orden.</summary>
public class OrdenServicioConcepto : BaseEntity
{
    public Guid OrdenServicioId { get; set; }
    public TipoConceptoOrden Tipo { get; set; }
    public Guid? RefaccionId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Importe { get; set; }

    /// <summary>Especialidad del taller a la que corresponde este trabajo (solo mano de obra),
    /// referencia al catálogo genérico (tipo "especialidades"). Permite medir la versatilidad
    /// de un técnico a través de las distintas áreas que domina.</summary>
    public Guid? EspecialidadId { get; set; }

    public OrdenServicio? OrdenServicio { get; set; }
}

/// <summary>Foto de evidencia subida por el mecánico/recepción, visible en el timeline público.</summary>
public class OrdenServicioFoto : BaseEntity
{
    public Guid OrdenServicioId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Nota { get; set; }
    public EstadoOrdenServicio EstadoAlSubir { get; set; }

    public OrdenServicio? OrdenServicio { get; set; }
}

/// <summary>Línea de tiempo de cambios de estatus (para el timeline del portal público).</summary>
public class OrdenServicioEstatusHistorial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenServicioId { get; set; }
    public EstadoOrdenServicio Estado { get; set; }
    public string? Comentario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public Guid? UsuarioId { get; set; }

    public OrdenServicio? OrdenServicio { get; set; }
}

/// <summary>Encuesta rápida de satisfacción, disponible cuando la orden está Entregado.</summary>
public class EncuestaSatisfaccion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenServicioId { get; set; }
    public int Calificacion { get; set; } // 1-5
    public string? Comentario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public OrdenServicio? OrdenServicio { get; set; }
}

public enum TipoMarcaDano
{
    Abolladura = 1,
    Rayon = 2,
    Golpe = 3,
    Rotura = 4,
    Otro = 5
}

/// <summary>
/// Marca de daño preexistente en el diagrama del vehículo (vista superior), capturada
/// en la recepción para proteger al taller y al cliente ante reclamos. Las coordenadas
/// (X, Y) son porcentajes (0-100) relativos al diagrama, para que se vean bien en
/// cualquier tamaño de pantalla.
/// </summary>
public class OrdenServicioMarcaDano
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenServicioId { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public TipoMarcaDano Tipo { get; set; } = TipoMarcaDano.Abolladura;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrdenServicio? OrdenServicio { get; set; }
}

/// <summary>
/// Relación muchos-a-muchos: los mecánicos (uno o varios) que atendieron una orden.
/// Permite analizar carga de trabajo y calidad/puntualidad por operador, incluso
/// cuando varias personas colaboraron en la misma reparación.
/// </summary>
public class OrdenServicioMecanico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenServicioId { get; set; }
    public Guid EmpleadoId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrdenServicio? OrdenServicio { get; set; }
}
