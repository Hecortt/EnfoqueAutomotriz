using EnfoqueAutomotriz.Domain.Promociones;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IPromocionRepository
{
    Task<List<Promocion>> GetAllAsync(bool soloVigentes = false, CancellationToken ct = default);
    Task<Promocion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task CreateAsync(Promocion promocion, CancellationToken ct = default);
    Task UpdateAsync(Promocion promocion, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
