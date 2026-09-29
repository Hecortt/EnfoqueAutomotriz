using EnfoqueAutomotriz.Domain.Auth;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface INotificacionRepository
{
    Task<List<Notificacion>> GetByUsuarioAsync(Guid usuarioId, bool soloNoLeidas = false, CancellationToken ct = default);
    Task<int> CountNoLeidasAsync(Guid usuarioId, CancellationToken ct = default);
    Task CreateAsync(Notificacion notificacion, CancellationToken ct = default);
    Task MarcarLeidaAsync(Guid id, CancellationToken ct = default);
    Task MarcarTodasLeidasAsync(Guid usuarioId, CancellationToken ct = default);

    /// <summary>Crea una notificación idéntica para todos los usuarios activos que tengan alguno de los roles indicados
    /// (ej. avisar a AdminTaller/SoporteIT de un evento de negocio). Útil cuando el evento no está ligado a un usuario único.</summary>
    Task CreateParaRolesAsync(int[] roles, string titulo, string mensaje, TipoNotificacion tipo, string? url = null, CancellationToken ct = default);
}
