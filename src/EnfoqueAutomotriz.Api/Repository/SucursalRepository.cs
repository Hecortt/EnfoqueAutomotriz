using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Sucursales;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class SucursalRepository : ISucursalRepository
{
    private readonly AppDbContext _db;
    public SucursalRepository(AppDbContext db) => _db = db;

    public Task<List<Sucursal>> GetAllAsync(CancellationToken ct = default) =>
        _db.Sucursales.OrderBy(s => s.Nombre).ToListAsync(ct);

    public Task<Sucursal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Sucursales.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task CreateAsync(Sucursal sucursal, CancellationToken ct = default)
    {
        _db.Sucursales.Add(sucursal);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Sucursal sucursal, CancellationToken ct = default)
    {
        sucursal.UpdatedAt = DateTime.UtcNow;
        _db.Sucursales.Update(sucursal);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _db.Sucursales.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s == null) return;
        s.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }
}
