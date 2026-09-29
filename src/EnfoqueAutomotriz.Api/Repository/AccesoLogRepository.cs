using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class AccesoLogRepository : IAccesoLogRepository
{
    private readonly AppDbContext _db;

    public AccesoLogRepository(AppDbContext db) => _db = db;

    public async Task RegistrarAccesoAsync(AccesoLog log, CancellationToken ct = default)
    {
        _db.AccesosLog.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<AccesoLog>> GetRecientesAsync(int take = 100, CancellationToken ct = default) =>
        _db.AccesosLog.OrderByDescending(a => a.Fecha).Take(take).ToListAsync(ct);
}
