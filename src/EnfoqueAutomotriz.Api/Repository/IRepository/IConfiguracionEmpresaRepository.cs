using EnfoqueAutomotriz.Domain.Configuracion;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IConfiguracionEmpresaRepository
{
    Task<ConfiguracionEmpresa?> GetAsync(CancellationToken ct = default);
    Task UpsertAsync(ConfiguracionEmpresa config, CancellationToken ct = default);
}
