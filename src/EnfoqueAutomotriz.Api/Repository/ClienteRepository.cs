using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Clientes;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _db;
    public ClienteRepository(AppDbContext db) => _db = db;

    public Task<List<Cliente>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Clientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Nombre.ToLower().Contains(s) || (c.Telefono != null && c.Telefono.Contains(s)));
        }
        return query.OrderByDescending(c => c.UltimaVisita).ThenBy(c => c.Nombre).ToListAsync(ct);
    }

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cliente?> GetWithVehiculosAsync(Guid id, CancellationToken ct = default) =>
        _db.Clientes.Include(c => c.Vehiculos).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task CreateAsync(Cliente cliente, CancellationToken ct = default)
    {
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Cliente cliente, CancellationToken ct = default)
    {
        cliente.UpdatedAt = DateTime.UtcNow;
        _db.Clientes.Update(cliente);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.Clientes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c == null) return;
        c.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RegistrarVisitaAsync(Guid clienteId, CancellationToken ct = default)
    {
        var c = await _db.Clientes.FirstOrDefaultAsync(x => x.Id == clienteId, ct);
        if (c == null) return;
        c.NumeroVisitas++;
        c.PrimeraVisita ??= DateTime.UtcNow;
        c.UltimaVisita = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Vehiculo>> GetVehiculosByClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        _db.Vehiculos.Where(v => v.ClienteId == clienteId).OrderBy(v => v.Marca).ToListAsync(ct);

    public Task<Vehiculo?> GetVehiculoByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Vehiculos.Include(v => v.Cliente).FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task CreateVehiculoAsync(Vehiculo vehiculo, CancellationToken ct = default)
    {
        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateVehiculoAsync(Vehiculo vehiculo, CancellationToken ct = default)
    {
        vehiculo.UpdatedAt = DateTime.UtcNow;
        _db.Vehiculos.Update(vehiculo);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Vehiculo>> GetAllVehiculosAsync(CancellationToken ct = default) =>
        _db.Vehiculos.Include(v => v.Cliente).OrderBy(v => v.Marca).ToListAsync(ct);
}
