using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Auth;

public enum TipoNotificacion
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Error = 4
}

/// <summary>Notificación dirigida a un usuario específico, mostrada en el centro de
/// notificaciones del header (campana). Generada por eventos del sistema.</summary>
public class Notificacion : BaseEntity
{
    public Guid UsuarioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public TipoNotificacion Tipo { get; set; } = TipoNotificacion.Info;
    public string? Url { get; set; }
    public bool Leida { get; set; }
    public DateTime? FechaLeida { get; set; }

    public Usuario? Usuario { get; set; }
}
