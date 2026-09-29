using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Comunicacion;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

/// <summary>
/// Motor de plantillas + historial de mensajes al cliente (fase 1: simulado, sin
/// integración real de WhatsApp/SMS/Email — se conecta a un proveedor en fase 2).
/// </summary>
public static class ComunicacionEndpoints
{
    public static RouteGroupBuilder MapComunicacionEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/comunicacion").WithTags("Comunicación").RequireAuthorization();

        g.MapGet("/plantillas", async (IComunicacionRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<PlantillaMensaje>>.Success(await repo.GetPlantillasAsync(ct))))
            .MapToApiVersion(v1);

        g.MapPost("/plantillas", async ([FromBody] PlantillaMensaje dto, IComunicacionRepository repo, CancellationToken ct) =>
        {
            await repo.CreatePlantillaAsync(dto, ct);
            return Results.Created($"/api/v1/comunicacion/plantillas/{dto.Id}", APIResponse<PlantillaMensaje>.Success(dto));
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        g.MapGet("/historial", async ([FromQuery] Guid? clienteId, IComunicacionRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<MensajeEnviado>>.Success(await repo.GetHistorialAsync(clienteId, ct))))
            .MapToApiVersion(v1);

        g.MapPost("/enviar", async ([FromBody] EnviarMensajeDto dto, IComunicacionRepository repo, CancellationToken ct) =>
        {
            var mensaje = await repo.EnviarSimuladoAsync(dto.ClienteId, dto.OrdenServicioId, dto.PlantillaId, dto.Destinatario, dto.Placeholders, ct);
            return Results.Ok(APIResponse<MensajeEnviado>.Success(mensaje));
        }).MapToApiVersion(v1);

        return g;
    }
}

public record EnviarMensajeDto(Guid ClienteId, Guid? OrdenServicioId, Guid PlantillaId, string Destinatario, Dictionary<string, string> Placeholders);
