using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Promociones;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class PromocionRepository : IPromocionRepository
{
    private readonly AppDbContext _db;
    public PromocionRepository(AppDbContext db) => _db = db;

    public async Task<List<Promocion>> GetAllAsync(bool soloVigentes = false, CancellationToken ct = default)
    {
        var promos = await _db.Promociones.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
        return soloVigentes ? promos.Where(p => p.Vigente).ToList() : promos;
    }

    public Task<Promocion?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Promociones.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task CreateAsync(Promocion promocion, CancellationToken ct = default)
    {
        _db.Promociones.Add(promocion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Promocion promocion, CancellationToken ct = default)
    {
        promocion.UpdatedAt = DateTime.UtcNow;
        _db.Promociones.Update(promocion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.Promociones.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null) return;
        p.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }
}
