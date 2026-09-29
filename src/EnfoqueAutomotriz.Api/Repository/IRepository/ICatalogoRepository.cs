using EnfoqueAutomotriz.Domain.Catalogs;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface ICatalogoRepository
{
    Task<List<Catalogo>> GetByTipoAsync(string tipo, CancellationToken ct = default);
    Task<Catalogo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task CreateAsync(Catalogo catalogo, CancellationToken ct = default);
    Task UpdateAsync(Catalogo catalogo, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
