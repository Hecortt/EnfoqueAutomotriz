using EnfoqueAutomotriz.Domain.Clientes;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IClienteRepository
{
    Task<List<Cliente>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cliente?> GetWithVehiculosAsync(Guid id, CancellationToken ct = default);
    Task CreateAsync(Cliente cliente, CancellationToken ct = default);
    Task UpdateAsync(Cliente cliente, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task RegistrarVisitaAsync(Guid clienteId, CancellationToken ct = default);

    Task<List<Vehiculo>> GetVehiculosByClienteAsync(Guid clienteId, CancellationToken ct = default);
    Task<Vehiculo?> GetVehiculoByIdAsync(Guid id, CancellationToken ct = default);
    Task CreateVehiculoAsync(Vehiculo vehiculo, CancellationToken ct = default);
    Task UpdateVehiculoAsync(Vehiculo vehiculo, CancellationToken ct = default);
    Task<List<Vehiculo>> GetAllVehiculosAsync(CancellationToken ct = default);
}
