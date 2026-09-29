using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Clientes;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class ClientesEndpoints
{
    public static RouteGroupBuilder MapClientesEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/clientes").WithTags("Clientes").RequireAuthorization();

        g.MapGet("/", async ([FromQuery] string? q, IClienteRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Cliente>>.Success(await repo.GetAllAsync(q, ct))))
            .MapToApiVersion(v1);

        g.MapGet("/{id:guid}", async (Guid id, IClienteRepository repo, CancellationToken ct) =>
        {
            var c = await repo.GetWithVehiculosAsync(id, ct);
            return c == null ? Results.NotFound(APIResponse.Failure("No encontrado")) : Results.Ok(APIResponse<Cliente>.Success(c));
        }).MapToApiVersion(v1);

        g.MapPost("/", async ([FromBody] Cliente dto, IClienteRepository repo, CancellationToken ct) =>
        {
            await repo.CreateAsync(dto, ct);
            return Results.Created($"/api/v1/clientes/{dto.Id}", APIResponse<Cliente>.Success(dto));
        }).MapToApiVersion(v1);

        g.MapPut("/{id:guid}", async (Guid id, [FromBody] Cliente dto, IClienteRepository repo, CancellationToken ct) =>
        {
            var c = await repo.GetByIdAsync(id, ct);
            if (c == null) return Results.NotFound(APIResponse.Failure("No encontrado"));
            c.Nombre = dto.Nombre; c.Telefono = dto.Telefono; c.Email = dto.Email;
            c.Rfc = dto.Rfc; c.Direccion = dto.Direccion; c.Notas = dto.Notas; c.Activo = dto.Activo;
            await repo.UpdateAsync(c, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapDelete("/{id:guid}", async (Guid id, IClienteRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1).RequireAuthorization("AccesoTotal");

        // ── Vehículos ────────────────────────────────────────────────────────
        g.MapGet("/{clienteId:guid}/vehiculos", async (Guid clienteId, IClienteRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Vehiculo>>.Success(await repo.GetVehiculosByClienteAsync(clienteId, ct))))
            .MapToApiVersion(v1);

        g.MapPost("/{clienteId:guid}/vehiculos", async (Guid clienteId, [FromBody] Vehiculo dto, IClienteRepository repo, CancellationToken ct) =>
        {
            dto.ClienteId = clienteId;
            await repo.CreateVehiculoAsync(dto, ct);
            return Results.Created($"/api/v1/clientes/{clienteId}/vehiculos/{dto.Id}", APIResponse<Vehiculo>.Success(dto));
        }).MapToApiVersion(v1);

        var vehiculos = api.MapGroup("/vehiculos").WithTags("Vehículos").RequireAuthorization();

        vehiculos.MapGet("/", async (IClienteRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Vehiculo>>.Success(await repo.GetAllVehiculosAsync(ct))))
            .MapToApiVersion(v1);

        vehiculos.MapGet("/{id:guid}", async (Guid id, IClienteRepository repo, CancellationToken ct) =>
        {
            var v = await repo.GetVehiculoByIdAsync(id, ct);
            return v == null ? Results.NotFound(APIResponse.Failure("No encontrado")) : Results.Ok(APIResponse<Vehiculo>.Success(v));
        }).MapToApiVersion(v1);

        vehiculos.MapPut("/{id:guid}", async (Guid id, [FromBody] Vehiculo dto, IClienteRepository repo, CancellationToken ct) =>
        {
            var v = await repo.GetVehiculoByIdAsync(id, ct);
            if (v == null) return Results.NotFound(APIResponse.Failure("No encontrado"));
            v.Marca = dto.Marca; v.Modelo = dto.Modelo; v.Anio = dto.Anio; v.Placas = dto.Placas;
            v.Color = dto.Color; v.Vin = dto.Vin; v.Kilometraje = dto.Kilometraje; v.Activo = dto.Activo;
            await repo.UpdateVehiculoAsync(v, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        return g;
    }
}
