using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly AppDbContext _db;

    public AuditoriaRepository(AppDbContext db) => _db = db;

    public Task<List<AuditoriaGeneral>> GetRecientesAsync(int take = 100, CancellationToken ct = default) =>
        _db.AuditoriaGeneral.OrderByDescending(a => a.Fecha).Take(take).ToListAsync(ct);

    public Task<List<AuditoriaGeneral>> GetByEntidadAsync(string entidad, string entidadId, CancellationToken ct = default) =>
        _db.AuditoriaGeneral
            .Where(a => a.Entidad == entidad && a.EntidadId == entidadId)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync(ct);
}
