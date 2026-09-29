using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Configuracion;

/// <summary>Datos generales del tenant (taller): identidad, contacto, branding y facturación.</summary>
public class ConfiguracionEmpresa : BaseEntity
{
    public string NombreComercial { get; set; } = "Mi Taller";
    public string? RazonSocial { get; set; }
    public string? Rfc { get; set; }
    public string? LogoUrl { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? SitioWeb { get; set; }
    public string? Banco { get; set; }
    public string? Clabe { get; set; }
    public string? CuentaBancaria { get; set; }
    public string? TitularCuenta { get; set; }
    public string Moneda { get; set; } = "MXN";
    public string? LeyendaFactura { get; set; }
}
