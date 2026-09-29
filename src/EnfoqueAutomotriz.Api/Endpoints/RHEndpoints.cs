using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.RH;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class RHEndpoints
{
    public static RouteGroupBuilder MapRHEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/rh").WithTags("RH").RequireAuthorization("AccesoTotal");

        g.MapGet("/empleados", async (IEmpleadoRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Empleado>>.Success(await repo.GetAllAsync(ct))))
            .MapToApiVersion(v1);

        g.MapGet("/empleados/{id:guid}", async (Guid id, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            var e = await repo.GetByIdAsync(id, ct);
            return e == null ? Results.NotFound(APIResponse.Failure("No encontrado")) : Results.Ok(APIResponse<Empleado>.Success(e));
        }).MapToApiVersion(v1);

        g.MapPost("/empleados", async ([FromBody] Empleado dto, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            dto.Codigo = await repo.GenerarCodigoAsync(ct);
            await repo.CreateAsync(dto, ct);
            return Results.Created($"/api/v1/rh/empleados/{dto.Id}", APIResponse<Empleado>.Success(dto));
        }).MapToApiVersion(v1);

        g.MapPut("/empleados/{id:guid}", async (Guid id, [FromBody] Empleado dto, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            var e = await repo.GetByIdAsync(id, ct);
            if (e == null) return Results.NotFound(APIResponse.Failure("No encontrado"));
            e.Nombres = dto.Nombres; e.ApellidoPaterno = dto.ApellidoPaterno; e.ApellidoMaterno = dto.ApellidoMaterno;
            e.Telefono = dto.Telefono; e.Email = dto.Email; e.SalarioDiario = dto.SalarioDiario;
            e.SucursalId = dto.SucursalId; e.PuestoId = dto.PuestoId; e.JefeDirectoId = dto.JefeDirectoId;
            e.FotoUrl = dto.FotoUrl; e.Activo = dto.Activo;
            await repo.UpdateAsync(e, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapGet("/organigrama", async (IEmpleadoRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<OrganigramaNodo>>.Success(await repo.GetOrganigramaAsync(ct))))
            .MapToApiVersion(v1);

        g.MapGet("/departamentos", async (IEmpleadoRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Departamento>>.Success(await repo.GetDepartamentosAsync(ct))))
            .MapToApiVersion(v1);

        g.MapPost("/departamentos", async ([FromBody] Departamento dto, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            await repo.CreateDepartamentoAsync(dto, ct);
            return Results.Created($"/api/v1/rh/departamentos/{dto.Id}", APIResponse<Departamento>.Success(dto));
        }).MapToApiVersion(v1);

        g.MapGet("/puestos", async ([FromQuery] Guid? departamentoId, IEmpleadoRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Puesto>>.Success(await repo.GetPuestosAsync(departamentoId, ct))))
            .MapToApiVersion(v1);

        g.MapPost("/puestos", async ([FromBody] Puesto dto, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            await repo.CreatePuestoAsync(dto, ct);
            return Results.Created($"/api/v1/rh/puestos/{dto.Id}", APIResponse<Puesto>.Success(dto));
        }).MapToApiVersion(v1);

        // ── Capacitación ──────────────────────────────────────────────────
        g.MapGet("/capacitaciones", async (IEmpleadoRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<Capacitacion>>.Success(await repo.GetCapacitacionesAsync(ct))))
            .MapToApiVersion(v1);

        g.MapPost("/capacitaciones", async ([FromBody] CapacitacionCreateDto dto, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            var capacitacion = new Capacitacion
            {
                Titulo = dto.Titulo,
                Descripcion = dto.Descripcion,
                Categoria = dto.Categoria,
                DuracionHoras = dto.DuracionHoras
            };
            await repo.CreateCapacitacionAsync(capacitacion, ct);
            return Results.Created($"/api/v1/rh/capacitaciones/{capacitacion.Id}", APIResponse<Capacitacion>.Success(capacitacion));
        }).MapToApiVersion(v1);

        g.MapPut("/capacitaciones/{id:guid}", async (Guid id, [FromBody] CapacitacionCreateDto dto, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            var existentes = await repo.GetCapacitacionesAsync(ct);
            var capacitacion = existentes.FirstOrDefault(c => c.Id == id);
            if (capacitacion == null) return Results.NotFound(APIResponse.Failure("No encontrada"));

            capacitacion.Titulo = dto.Titulo;
            capacitacion.Descripcion = dto.Descripcion;
            capacitacion.Categoria = dto.Categoria;
            capacitacion.DuracionHoras = dto.DuracionHoras;
            await repo.UpdateCapacitacionAsync(capacitacion, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapGet("/capacitaciones/asignaciones", async ([FromQuery] Guid? empleadoId, [FromQuery] Guid? capacitacionId,
            IEmpleadoRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<EmpleadoCapacitacion>>.Success(await repo.GetAsignacionesAsync(empleadoId, capacitacionId, ct))))
            .MapToApiVersion(v1);

        g.MapPost("/capacitaciones/asignaciones", async ([FromBody] AsignarCapacitacionDto dto, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            try
            {
                var creada = await repo.AsignarCapacitacionAsync(dto.EmpleadoId, dto.CapacitacionId, ct);
                return Results.Created($"/api/v1/rh/capacitaciones/asignaciones/{creada.Id}", APIResponse<EmpleadoCapacitacion>.Success(creada));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(APIResponse.Failure(ex.Message));
            }
        }).MapToApiVersion(v1);

        g.MapPatch("/capacitaciones/asignaciones/{id:guid}", async (Guid id, [FromBody] ActualizarAsignacionDto dto, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            await repo.ActualizarEstadoAsignacionAsync(id, dto.Estado, dto.CalificacionFinal, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapDelete("/capacitaciones/asignaciones/{id:guid}", async (Guid id, IEmpleadoRepository repo, CancellationToken ct) =>
        {
            await repo.EliminarAsignacionAsync(id, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        return g;
    }
}

public record CapacitacionCreateDto(string Titulo, string? Descripcion, string? Categoria, int DuracionHoras);

public record AsignarCapacitacionDto(Guid EmpleadoId, Guid CapacitacionId);

public record ActualizarAsignacionDto(EstadoCapacitacionEmpleado Estado, decimal? CalificacionFinal);
