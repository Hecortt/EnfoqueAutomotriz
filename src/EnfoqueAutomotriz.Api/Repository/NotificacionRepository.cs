using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class NotificacionRepository : INotificacionRepository
{
    private readonly AppDbContext _db;

    public NotificacionRepository(AppDbContext db) => _db = db;

    public Task<List<Notificacion>> GetByUsuarioAsync(Guid usuarioId, bool soloNoLeidas = false, CancellationToken ct = default)
    {
        var query = _db.Notificaciones.Where(n => n.UsuarioId == usuarioId);
        if (soloNoLeidas) query = query.Where(n => !n.Leida);
        return query.OrderByDescending(n => n.CreatedAt).ToListAsync(ct);
    }

    public Task<int> CountNoLeidasAsync(Guid usuarioId, CancellationToken ct = default) =>
        _db.Notificaciones.CountAsync(n => n.UsuarioId == usuarioId && !n.Leida, ct);

    public async Task CreateAsync(Notificacion notificacion, CancellationToken ct = default)
    {
        _db.Notificaciones.Add(notificacion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarcarLeidaAsync(Guid id, CancellationToken ct = default)
    {
        var notif = await _db.Notificaciones.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (notif == null) return;
        notif.Leida = true;
        notif.FechaLeida = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarcarTodasLeidasAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var pendientes = await _db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && !n.Leida)
            .ToListAsync(ct);

        foreach (var n in pendientes)
        {
            n.Leida = true;
            n.FechaLeida = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task CreateParaRolesAsync(int[] roles, string titulo, string mensaje, TipoNotificacion tipo, string? url = null, CancellationToken ct = default)
    {
        var usuarioIds = await _db.Usuarios
            .Where(u => u.Activo && roles.Contains(u.Rol))
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (usuarioIds.Count == 0) return;

        foreach (var usuarioId in usuarioIds)
        {
            _db.Notificaciones.Add(new Notificacion
            {
                UsuarioId = usuarioId,
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = tipo,
                Url = url
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
