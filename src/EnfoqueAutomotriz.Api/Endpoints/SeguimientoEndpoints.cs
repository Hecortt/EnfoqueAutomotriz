using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Api.Endpoints;

/// <summary>
/// Endpoints públicos (sin autenticación) para que el cliente dé seguimiento a su
/// orden de servicio vía el token único (QR del ticket impreso o link enviado por
/// el bot), sin necesidad de crear una cuenta.
/// </summary>
public static class SeguimientoEndpoints
{
    public static RouteGroupBuilder MapSeguimientoEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/seguimiento").WithTags("Seguimiento Público").AllowAnonymous();

        g.MapGet("/{token:guid}", async (Guid token, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var orden = await repo.GetByTokenAsync(token, ct);
            if (orden == null) return Results.NotFound(APIResponse.Failure("No se encontró la orden."));

            return Results.Ok(APIResponse<object>.Success(new
            {
                orden.Folio,
                orden.Estado,
                orden.EstadoCotizacion,
                orden.FechaRecepcion,
                orden.FechaEntrega,
                orden.ProblemaReportado,
                orden.Subtotal,
                orden.Descuento,
                orden.Iva,
                orden.Total,
                orden.Anticipo,
                orden.Saldo,
                Cliente = orden.Cliente?.Nombre,
                Vehiculo = orden.Vehiculo == null ? null : $"{orden.Vehiculo.Marca} {orden.Vehiculo.Modelo} {orden.Vehiculo.Anio} — {orden.Vehiculo.Placas}",
                Sucursal = orden.Sucursal?.Nombre,
                Conceptos = orden.Conceptos.Where(c => !c.IsDeleted).Select(c => new { c.Descripcion, c.Cantidad, c.PrecioUnitario, c.Importe }),
                Fotos = orden.Fotos.OrderBy(f => f.CreatedAt).Select(f => new { f.Url, f.Nota, f.EstadoAlSubir, f.CreatedAt }),
                Timeline = orden.Historial.OrderBy(h => h.Fecha).Select(h => new { h.Estado, h.Comentario, h.Fecha })
            }));
        }).MapToApiVersion(v1);

        g.MapPatch("/{token:guid}/aprobar", async (Guid token, IOrdenServicioRepository repo, IAlmacenRepository almacenRepo, CancellationToken ct) =>
        {
            var ok = await repo.AprobarCotizacionAsync(token, ct);
            if (!ok) return Results.BadRequest(APIResponse.Failure("No hay una cotización pendiente de aprobación."));

            // Dispara automáticamente la solicitud de preparación a almacén con las refacciones cotizadas.
            var orden = await repo.GetByTokenAsync(token, ct);
            if (orden != null) await almacenRepo.CrearSolicitudDesdeOrdenAsync(orden.Id, ct);

            return Results.Ok(APIResponse.Success("Cotización aprobada. ¡Gracias! Ya estamos trabajando en tu vehículo."));
        }).MapToApiVersion(v1);

        g.MapPatch("/{token:guid}/rechazar", async (Guid token, RechazarDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var ok = await repo.RechazarCotizacionAsync(token, dto.Comentario, ct);
            if (!ok) return Results.BadRequest(APIResponse.Failure("No hay una cotización pendiente de aprobación."));
            return Results.Ok(APIResponse.Success("Hemos registrado tu decisión. Un asesor se pondrá en contacto contigo."));
        }).MapToApiVersion(v1);

        g.MapPost("/{token:guid}/encuesta", async (Guid token, EncuestaDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var orden = await repo.GetByTokenAsync(token, ct);
            if (orden == null) return Results.NotFound(APIResponse.Failure("No se encontró la orden."));
            await repo.RegistrarEncuestaAsync(orden.Id, dto.Calificacion, dto.Comentario, ct);
            return Results.Ok(APIResponse.Success("¡Gracias por tu opinión!"));
        }).MapToApiVersion(v1);

        // ── Portal de Entrega público: mismo token, ficha completa para presentar al cliente ──
        g.MapGet("/{token:guid}/entrega", async (Guid token, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var orden = await repo.GetByTokenAsync(token, ct);
            if (orden == null) return Results.NotFound(APIResponse.Failure("No se encontró la orden."));
            var data = await repo.GetPortalEntregaAsync(orden.Id, ct);
            return data == null ? Results.NotFound(APIResponse.Failure("No se encontró la orden.")) : Results.Ok(APIResponse<object>.Success(data));
        }).MapToApiVersion(v1);

        g.MapPost("/{token:guid}/entrega/encuesta", async (Guid token, EncuestaDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var orden = await repo.GetByTokenAsync(token, ct);
            if (orden == null) return Results.NotFound(APIResponse.Failure("No se encontró la orden."));
            await repo.RegistrarEncuestaAsync(orden.Id, dto.Calificacion, dto.Comentario, ct);
            return Results.Ok(APIResponse.Success("¡Gracias por tu opinión!"));
        }).MapToApiVersion(v1);

        return g;
    }
}

public record RechazarDto(string? Comentario);
public record EncuestaDto(int Calificacion, string? Comentario);
