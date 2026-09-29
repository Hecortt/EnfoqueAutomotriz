namespace EnfoqueAutomotriz.Utility;

public static class SD
{
    public const string AccessToken = "JWTToken";
    public const string RefreshToken = "RefreshToken";

    public const string ClienteIdClaim = "ClienteId";

    public enum Roles
    {
        SuperAdmin = 0,        // Administración SaaS central (todos los talleres)
        AdminTaller = 1,       // Dueño / gerente del taller (tenant)
        Mecanico = 2,          // Técnico / mecánico
        Recepcionista = 3,     // Asesor de servicio / recepción
        Enfermeria = 4,        // Reservado (heredado del patrón EnfoqueMedico) — no usado en taller
        Facturacion = 5,       // Facturación / contador
        SoporteIT = 6          // Soporte técnico interno
    }

    /// <summary>Obtiene el nombre descriptivo de un rol</summary>
    public static string GetRoleName(int rol) => rol switch
    {
        0 => "Super Admin",
        1 => "Admin Taller",
        2 => "Mecánico",
        3 => "Recepcionista",
        4 => "Enfermería",
        5 => "Facturación",
        6 => "Soporte IT",
        _ => "Desconocido"
    };

    /// <summary>Obtiene el color hexadecimal para un rol (para badges)</summary>
    public static string GetRolColor(int rol) => rol switch
    {
        0 => "#B91C1C",  // Dark Red
        1 => "#7C3AED",  // Purple
        2 => "#2563EB",  // Blue
        3 => "#059669",  // Green
        4 => "#F59E0B",  // Amber
        5 => "#0891B2",  // Cyan
        6 => "#DC2626",  // Red
        _ => "#6B7280"   // Gray
    };

    public enum ApiType
    {
        GET,
        POST,
        PUT,
        PATCH,
        DELETE
    }

    public enum ContentType
    {
        Json,
        MultipartFormData
    }

    public static class FileSettings
    {
        public const int MaxPhotoSizeMB = 2;
        public const int MaxDocumentSizeMB = 5;

        public static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        public static readonly string[] AllowedDocumentExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"];

        public const string UploadPath = "uploads";
        public const string LogoUploadPath = "uploads/logos";
        public const int LogoMaxWidth = 600;
        public const int LogoMaxHeight = 600;
    }

    /// <summary>
    /// Tipos de catálogo genérico usados por el módulo transversal `Catalogo`
    /// (ver EnfoqueAutomotriz.Domain.Catalogs.Catalogo). Los catálogos específicos
    /// de negocio (especialidades, tipos de servicio, marcas, etc.) se agregarán
    /// en la fase de diseño del dominio automotriz.
    /// </summary>
    public static class CatalogTypes
    {
        public const string Especialidades = "especialidades";
        public const string TiposServicio = "tipos_servicio";
    }
}
