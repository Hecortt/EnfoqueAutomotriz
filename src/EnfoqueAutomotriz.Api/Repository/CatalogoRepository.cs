using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class CatalogoRepository : ICatalogoRepository
{
    private readonly AppDbContext _db;

    public CatalogoRepository(AppDbContext db) => _db = db;

    public Task<List<Catalogo>> GetByTipoAsync(string tipo, CancellationToken ct = default) =>
        _db.Catalogos.Where(c => c.Tipo == tipo).OrderBy(c => c.Orden).ToListAsync(ct);

    public Task<Catalogo?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Catalogos.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task CreateAsync(Catalogo catalogo, CancellationToken ct = default)
    {
        _db.Catalogos.Add(catalogo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Catalogo catalogo, CancellationToken ct = default)
    {
        catalogo.UpdatedAt = DateTime.UtcNow;
        _db.Catalogos.Update(catalogo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var catalogo = await _db.Catalogos.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (catalogo == null) return;
        catalogo.IsDeleted = true;
        catalogo.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
