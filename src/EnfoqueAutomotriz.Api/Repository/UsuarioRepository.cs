using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _db;

    public UsuarioRepository(AppDbContext db) => _db = db;

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim(), ct);

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<List<Usuario>> GetAllAsync(CancellationToken ct = default) =>
        _db.Usuarios.OrderBy(u => u.NombreCompleto).ToListAsync(ct);

    public async Task CreateAsync(Usuario usuario, CancellationToken ct = default)
    {
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Usuario usuario, CancellationToken ct = default)
    {
        usuario.UpdatedAt = DateTime.UtcNow;
        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario == null) return;
        usuario.IsDeleted = true;
        usuario.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task IncrementFailedAttemptsAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario == null) return;

        usuario.IntentosFallidos++;
        if (usuario.IntentosFallidos >= 5)
        {
            usuario.Bloqueado = true;
            usuario.FechaBloqueo = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateLastAccessAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (usuario == null) return;

        usuario.UltimoAcceso = DateTime.UtcNow;
        usuario.IntentosFallidos = 0;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(int total, int legacy)> GetHashMigrationStatusAsync(CancellationToken ct = default)
    {
        var total = await _db.Usuarios.CountAsync(ct);
        var legacy = await _db.Usuarios.CountAsync(u => !u.PasswordHash.StartsWith("v1."), ct);
        return (total, legacy);
    }
}
