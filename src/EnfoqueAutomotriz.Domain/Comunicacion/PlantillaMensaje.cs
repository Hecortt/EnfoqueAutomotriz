using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Comunicacion;

public enum CanalMensaje { WhatsApp = 1, Email = 2, Sms = 3 }
public enum EstadoMensajeEnviado { Simulado = 1, Enviado = 2, Fallido = 3 }

/// <summary>
/// Plantilla de mensaje al cliente (ej. "Tu vehículo está listo"). Fase 1: motor de
/// plantillas + historial, sin integración real de envío (se conecta a un proveedor
/// de WhatsApp/SMS/Email en una fase posterior).
/// </summary>
public class PlantillaMensaje : CatalogEntity
{
    public string? Asunto { get; set; }
    public string Cuerpo { get; set; } = string.Empty; // admite placeholders {{cliente}}, {{vehiculo}}, {{folio}}
    public CanalMensaje Canal { get; set; } = CanalMensaje.WhatsApp;
}

public class MensajeEnviado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public Guid? OrdenServicioId { get; set; }
    public Guid? PlantillaMensajeId { get; set; }
    public CanalMensaje Canal { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public EstadoMensajeEnviado Estado { get; set; } = EstadoMensajeEnviado.Simulado;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
