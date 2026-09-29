using EnfoqueAutomotriz.Domain.Auth;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IPermisoRolRepository
{
    Task<List<PermisoRol>> GetByRolAsync(int rol, CancellationToken ct = default);
    Task<List<PermisoRol>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(ActualizarPermisoRolDto dto, CancellationToken ct = default);
}
