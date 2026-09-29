using EnfoqueAutomotriz.Domain.Auth;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IAccesoLogRepository
{
    Task RegistrarAccesoAsync(AccesoLog log, CancellationToken ct = default);
    Task<List<AccesoLog>> GetRecientesAsync(int take = 100, CancellationToken ct = default);
}
