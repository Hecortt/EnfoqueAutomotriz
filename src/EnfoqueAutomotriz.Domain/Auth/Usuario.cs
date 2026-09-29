using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Auth;

public class Usuario : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Rol { get; set; }
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; } = true;
    public bool Bloqueado { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime? FechaBloqueo { get; set; }
}
