namespace EnfoqueAutomotriz.Domain.Auth;

/// <summary>
/// Define los permisos/módulos que cada rol puede acceder.
/// Permite gestión dinámica de permisos sin recompilar.
/// </summary>
public class PermisoRol
{
    public Guid Id { get; set; }
    public int Rol { get; set; } // 0-6, ver SD.Roles
    public string Modulo { get; set; } = string.Empty; // "Dashboard", "OrdenesServicio", "Inventario", etc.
    public string? Descripcion { get; set; }
    public bool PuedeVer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeEliminar { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}

public class PermisoRolDto
{
    public Guid Id { get; set; }
    public int Rol { get; set; }
    public string Modulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool PuedeVer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeEliminar { get; set; }
}

public class ActualizarPermisoRolDto
{
    public Guid Id { get; set; }
    public bool PuedeVer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeEliminar { get; set; }
}
