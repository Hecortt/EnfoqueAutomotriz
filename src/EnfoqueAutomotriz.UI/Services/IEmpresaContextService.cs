namespace EnfoqueAutomotriz.UI.Services;

public record EmpresaContext(
    string NombreComercial,
    string? LogoUrl,
    string? RazonSocial
);

public interface IEmpresaContextService
{
    Task<EmpresaContext> GetAsync(CancellationToken ct = default);
    void Invalidar();
}
