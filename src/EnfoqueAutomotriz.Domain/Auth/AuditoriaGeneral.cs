using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Auth;

public enum AccionAuditoria
{
    Crear = 1,
    Modificar = 2,
    Eliminar = 3
}

public class AuditoriaGeneral : BaseEntity
{
    public Guid? UsuarioId { get; set; }
    public string? UsuarioEmail { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public AccionAuditoria Accion { get; set; }
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? IP { get; set; }
    public string? Modulo { get; set; }

    public Usuario? Usuario { get; set; }
}
