using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Auth;
using EnfoqueAutomotriz.Domain.Shared;
using System.Security.Claims;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class NotificacionesEndpoints
{
    public static RouteGroupBuilder MapNotificacionesEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var notif = api.MapGroup("/notificaciones").WithTags("Notificaciones").RequireAuthorization();

        notif.MapGet("/", async (ClaimsPrincipal user, INotificacionRepository repo, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var items = await repo.GetByUsuarioAsync(userId.Value, false, ct);
            return Results.Ok(APIResponse<List<Notificacion>>.Success(items));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<List<Notificacion>>>();

        notif.MapGet("/recientes", async (ClaimsPrincipal user, INotificacionRepository repo, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var items = await repo.GetByUsuarioAsync(userId.Value, false, ct);
            return Results.Ok(APIResponse<List<Notificacion>>.Success(items.Take(20).ToList()));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<List<Notificacion>>>();

        notif.MapGet("/no-leidas-count", async (ClaimsPrincipal user, INotificacionRepository repo, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            var count = await repo.CountNoLeidasAsync(userId.Value, ct);
            return Results.Ok(APIResponse<object>.Success(new { count }));
        })
        .MapToApiVersion(v1)
        .Produces<APIResponse<object>>();

        notif.MapPost("/{id:guid}/leer", async (Guid id, INotificacionRepository repo, CancellationToken ct) =>
        {
            await repo.MarcarLeidaAsync(id, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1);

        notif.MapPatch("/{id:guid}/leer", async (Guid id, INotificacionRepository repo, CancellationToken ct) =>
        {
            await repo.MarcarLeidaAsync(id, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1);

        notif.MapPost("/leer-todas", async (ClaimsPrincipal user, INotificacionRepository repo, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            if (!userId.HasValue) return Results.Unauthorized();

            await repo.MarcarTodasLeidasAsync(userId.Value, ct);
            return Results.NoContent();
        })
        .MapToApiVersion(v1);

        return notif;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
