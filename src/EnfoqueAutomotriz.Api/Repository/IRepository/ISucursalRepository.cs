using EnfoqueAutomotriz.Domain.Sucursales;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface ISucursalRepository
{
    Task<List<Sucursal>> GetAllAsync(CancellationToken ct = default);
    Task<Sucursal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task CreateAsync(Sucursal sucursal, CancellationToken ct = default);
    Task UpdateAsync(Sucursal sucursal, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
