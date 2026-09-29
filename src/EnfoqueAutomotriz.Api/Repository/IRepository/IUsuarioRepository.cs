using EnfoqueAutomotriz.Domain.Auth;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Usuario>> GetAllAsync(CancellationToken ct = default);
    Task CreateAsync(Usuario usuario, CancellationToken ct = default);
    Task UpdateAsync(Usuario usuario, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task IncrementFailedAttemptsAsync(Guid id, CancellationToken ct = default);
    Task UpdateLastAccessAsync(Guid id, CancellationToken ct = default);
    Task<(int total, int legacy)> GetHashMigrationStatusAsync(CancellationToken ct = default);
}
