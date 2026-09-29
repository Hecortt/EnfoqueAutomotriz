using EnfoqueAutomotriz.Domain.Auth;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IAuditoriaRepository
{
    Task<List<AuditoriaGeneral>> GetRecientesAsync(int take = 100, CancellationToken ct = default);
    Task<List<AuditoriaGeneral>> GetByEntidadAsync(string entidad, string entidadId, CancellationToken ct = default);
}
