namespace EnfoqueAutomotriz.Domain.Auth;

public enum AccionAcceso
{
    Login = 1,
    Logout = 2,
    LoginFallido = 3,
    CambioPassword = 4,
    BloqueoUsuario = 5
}

public class AccesoLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public AccionAcceso Accion { get; set; }
    public string? IP { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public bool Exitoso { get; set; } = true;
    public string? Detalles { get; set; }

    public Usuario? Usuario { get; set; }
}
