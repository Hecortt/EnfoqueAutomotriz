namespace EnfoqueAutomotriz.Domain.Auth.DTOs;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? RecaptchaToken { get; set; }
}

public record LoginResponseDto(
    Guid Id,
    string Email,
    string NombreCompleto,
    int Rol,
    string? FotoUrl,
    string Token,
    DateTime Expiration,
    Guid? ClienteId
);

public class UsuarioDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public int Rol { get; set; }
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; }
    public bool Bloqueado { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}

public class UsuarioCreateDto
{
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Rol { get; set; }
    public Guid? ClienteId { get; set; }
    public Guid? OperadorId { get; set; }
}

public class UsuarioUpdateDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public int Rol { get; set; }
    public bool Activo { get; set; } = true;
    public Guid? OperadorId { get; set; }
}

public class ResetPasswordDto
{
    public string NuevoPassword { get; set; } = string.Empty;
}
