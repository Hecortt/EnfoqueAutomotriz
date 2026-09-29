using Asp.Versioning;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnfoqueAutomotriz.Api.Endpoints;

public static class OrdenesServicioEndpoints
{
    public static RouteGroupBuilder MapOrdenesServicioEndpoints(this RouteGroupBuilder api, ApiVersion v1)
    {
        var g = api.MapGroup("/ordenes-servicio").WithTags("Órdenes de Servicio").RequireAuthorization();

        g.MapGet("/", async ([FromQuery] Guid? sucursalId, [FromQuery] EstadoOrdenServicio? estado,
            IOrdenServicioRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<OrdenServicio>>.Success(await repo.GetAllAsync(sucursalId, estado, ct))))
            .MapToApiVersion(v1);

        g.MapGet("/{id:guid}", async (Guid id, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var o = await repo.GetByIdAsync(id, ct);
            return o == null ? Results.NotFound(APIResponse.Failure("No encontrada")) : Results.Ok(APIResponse<OrdenServicio>.Success(o));
        }).MapToApiVersion(v1);

        g.MapPost("/", async ([FromBody] OrdenServicioCreateDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var folio = await repo.GenerarFolioAsync(ct);
            var orden = new OrdenServicio
            {
                Folio = folio,
                SucursalId = dto.SucursalId,
                ClienteId = dto.ClienteId,
                VehiculoId = dto.VehiculoId,
                EmpleadoAsignadoId = dto.EmpleadoAsignadoId,
                ProblemaReportado = dto.ProblemaReportado,
                KilometrajeRecepcion = dto.KilometrajeRecepcion,
                NivelGasolina = dto.NivelGasolina,
                Observaciones = dto.Observaciones,
                FechaEntregaProgramada = dto.FechaEntregaProgramada
            };
            await repo.CreateAsync(orden, ct);
            return Results.Created($"/api/v1/ordenes-servicio/{orden.Id}", APIResponse<OrdenServicio>.Success(orden));
        }).MapToApiVersion(v1);

        g.MapPost("/{id:guid}/conceptos", async (Guid id, [FromBody] ConceptoCreateDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var concepto = new OrdenServicioConcepto
            {
                OrdenServicioId = id,
                Tipo = dto.Tipo,
                RefaccionId = dto.RefaccionId,
                Descripcion = dto.Descripcion,
                Cantidad = dto.Cantidad,
                PrecioUnitario = dto.PrecioUnitario,
                CostoUnitario = dto.CostoUnitario,
                EspecialidadId = dto.EspecialidadId
            };
            await repo.AddConceptoAsync(concepto, ct);
            return Results.Created($"/api/v1/ordenes-servicio/{id}/conceptos/{concepto.Id}", APIResponse<OrdenServicioConcepto>.Success(concepto));
        }).MapToApiVersion(v1);

        g.MapDelete("/conceptos/{conceptoId:guid}", async (Guid conceptoId, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            await repo.RemoveConceptoAsync(conceptoId, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapPost("/{id:guid}/fotos", async (Guid id, [FromBody] FotoCreateDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var foto = new OrdenServicioFoto
            {
                OrdenServicioId = id,
                Url = dto.Url,
                Nota = dto.Nota,
                EstadoAlSubir = dto.EstadoAlSubir
            };
            await repo.AddFotoAsync(foto, ct);
            return Results.Created($"/api/v1/ordenes-servicio/{id}/fotos/{foto.Id}", APIResponse<OrdenServicioFoto>.Success(foto));
        }).MapToApiVersion(v1);

        g.MapPatch("/{id:guid}/estado", async (Guid id, [FromBody] CambiarEstadoDto dto, IOrdenServicioRepository repo,
            ClaimsPrincipal user, CancellationToken ct) =>
        {
            await repo.CambiarEstadoAsync(id, dto.Estado, dto.Comentario, GetUserId(user), ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapPatch("/{id:guid}/fecha-programada", async (Guid id, [FromBody] FechaProgramadaDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            await repo.SetFechaEntregaProgramadaAsync(id, dto.FechaEntregaProgramada, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapPatch("/{id:guid}/problema-reportado", async (Guid id, [FromBody] ProblemaReportadoDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            await repo.UpdateProblemaReportadoAsync(id, dto.ProblemaReportado, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapPost("/{id:guid}/danos", async (Guid id, [FromBody] MarcaDanoCreateDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var marca = new OrdenServicioMarcaDano
            {
                OrdenServicioId = id,
                X = dto.X,
                Y = dto.Y,
                Tipo = dto.Tipo,
                Descripcion = dto.Descripcion
            };
            var creada = await repo.AddMarcaDanoAsync(marca, ct);
            return Results.Created($"/api/v1/ordenes-servicio/{id}/danos/{creada.Id}", APIResponse<OrdenServicioMarcaDano>.Success(creada));
        }).MapToApiVersion(v1);

        g.MapDelete("/danos/{marcaId:guid}", async (Guid marcaId, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            await repo.RemoveMarcaDanoAsync(marcaId, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapGet("/mecanicos-disponibles", async ([FromQuery] Guid? sucursalId, IOrdenServicioRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<object>>.Success(await repo.GetMecanicosDisponiblesAsync(sucursalId, ct))))
            .MapToApiVersion(v1);

        g.MapGet("/{id:guid}/mecanicos", async (Guid id, IOrdenServicioRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<List<object>>.Success(await repo.GetMecanicosAsync(id, ct))))
            .MapToApiVersion(v1);

        g.MapPost("/{id:guid}/mecanicos", async (Guid id, [FromBody] MecanicoAsignarDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            try
            {
                var creado = await repo.AddMecanicoAsync(id, dto.EmpleadoId, ct);
                return Results.Created($"/api/v1/ordenes-servicio/{id}/mecanicos", APIResponse<object>.Success(creado));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(APIResponse.Failure(ex.Message));
            }
        }).MapToApiVersion(v1);

        g.MapDelete("/mecanicos/{asignacionId:guid}", async (Guid asignacionId, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            await repo.RemoveMecanicoAsync(asignacionId, ct);
            return Results.NoContent();
        }).MapToApiVersion(v1);

        g.MapPost("/{id:guid}/enviar-cotizacion", async (Guid id, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            await repo.EnviarCotizacionAsync(id, ct);
            var orden = await repo.GetByIdAsync(id, ct);
            return Results.Ok(APIResponse<object>.Success(new
            {
                mensaje = "Cotización enviada. El cliente puede aprobarla desde el link de seguimiento.",
                linkSeguimiento = $"/seguimiento/{orden?.TokenSeguimiento}"
            }));
        }).MapToApiVersion(v1);

        g.MapGet("/{id:guid}/pdf", async (Guid id, IOrdenServicioRepository repo,
            Repository.IRepository.IConfiguracionEmpresaRepository empresaRepo, IWebHostEnvironment env,
            IHttpClientFactory httpFactory, ILogger<Program> logger, CancellationToken ct) =>
        {
            var orden = await repo.GetByIdAsync(id, ct);
            if (orden == null) return Results.NotFound(APIResponse.Failure("No encontrada"));

            var empresa = await empresaRepo.GetAsync(ct);
            byte[]? logoBytes = null;
            if (!string.IsNullOrEmpty(empresa?.LogoUrl))
            {
                if (empresa.LogoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    empresa.LogoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    // Logo enlazado a una imagen externa (ej. capturado en Configuración de la Empresa): se descarga
                    // para incrustarlo en el PDF; si falla (URL caída, timeout, etc.) simplemente no se muestra logo.
                    try
                    {
                        var client = httpFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(10);
                        logoBytes = await client.GetByteArrayAsync(empresa.LogoUrl, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "[PDF] No se pudo descargar el logo externo {LogoUrl}", empresa.LogoUrl);
                    }
                }
                else
                {
                    var path = Path.Combine(env.WebRootPath, empresa.LogoUrl.TrimStart('/'));
                    if (File.Exists(path)) logoBytes = await File.ReadAllBytesAsync(path, ct);
                }
            }

            var baseUrl = "http://localhost:5102"; // BaseUrl de la UI para el link del QR
            var pdfBytes = Helpers.OrdenServicioPdfGenerator.Generate(orden, empresa, logoBytes, baseUrl);
            return Results.File(pdfBytes, "application/pdf", $"{orden.Folio}.pdf");
        }).MapToApiVersion(v1);

        g.MapGet("/dashboard/kpis", async ([FromQuery] Guid? sucursalId, IOrdenServicioRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<object>.Success(await repo.GetDashboardKpisAsync(sucursalId, ct))))
            .MapToApiVersion(v1);

        g.MapGet("/dashboard/empresa", async (IOrdenServicioRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<object>.Success(await repo.GetDashboardEmpresaAsync(ct))))
            .MapToApiVersion(v1);

        g.MapGet("/dashboard/sucursal/{sucursalId:guid}", async (Guid sucursalId, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var data = await repo.GetDashboardSucursalAsync(sucursalId, ct);
            return data == null ? Results.NotFound(APIResponse.Failure("Sucursal no encontrada")) : Results.Ok(APIResponse<object>.Success(data));
        }).MapToApiVersion(v1);

        g.MapGet("/dashboard/calidad", async ([FromQuery] Guid? sucursalId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
            IOrdenServicioRepository repo, CancellationToken ct) =>
            Results.Ok(APIResponse<object>.Success(await repo.GetDashboardCalidadAsync(sucursalId, desde, hasta, ct))))
            .MapToApiVersion(v1);

        g.MapGet("/dashboard/desempeno/{empleadoId:guid}", async (Guid empleadoId, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var data = await repo.GetDesempenoTecnicoAsync(empleadoId, ct);
            return data == null ? Results.NotFound(APIResponse.Failure("Empleado no encontrado")) : Results.Ok(APIResponse<object>.Success(data));
        }).MapToApiVersion(v1);

        // ── Portal de Entrega (presentación en sala al momento de entregar el vehículo) ──
        g.MapGet("/{id:guid}/portal-entrega", async (Guid id, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var data = await repo.GetPortalEntregaAsync(id, ct);
            return data == null ? Results.NotFound(APIResponse.Failure("Orden no encontrada")) : Results.Ok(APIResponse<object>.Success(data));
        }).MapToApiVersion(v1);

        g.MapPost("/{id:guid}/encuesta", async (Guid id, [FromBody] EncuestaOrdenDto dto, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            await repo.RegistrarEncuestaAsync(id, dto.Calificacion, dto.Comentario, ct);
            return Results.Ok(APIResponse.Success("¡Gracias por tu opinión!"));
        }).MapToApiVersion(v1);

        // ── Agenda general del taller (calendario de recepciones y entregas) ──
        g.MapGet("/agenda", async ([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] Guid? sucursalId,
            [FromQuery] EstadoOrdenServicio? estado, IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var eventos = await repo.GetAgendaAsync(desde, hasta, sucursalId, estado, ct);
            return Results.Ok(APIResponse<List<object>>.Success(eventos));
        }).MapToApiVersion(v1);

        g.MapGet("/dashboard/presentacion", async (IOrdenServicioRepository repo, CancellationToken ct) =>
        {
            var data = await repo.GetResumenPresentacionAsync(ct);
            return Results.Ok(APIResponse<object>.Success(data));
        }).MapToApiVersion(v1);

        return g;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public record OrdenServicioCreateDto(
    Guid SucursalId, Guid ClienteId, Guid VehiculoId, Guid? EmpleadoAsignadoId,
    string ProblemaReportado, int KilometrajeRecepcion, string? NivelGasolina, string? Observaciones,
    DateTime? FechaEntregaProgramada = null
);

public record ConceptoCreateDto(
    TipoConceptoOrden Tipo, Guid? RefaccionId, string Descripcion,
    decimal Cantidad, decimal PrecioUnitario, decimal CostoUnitario,
    Guid? EspecialidadId = null
);

public record FotoCreateDto(string Url, string? Nota, EstadoOrdenServicio EstadoAlSubir);

public record CambiarEstadoDto(EstadoOrdenServicio Estado, string? Comentario);

public record FechaProgramadaDto(DateTime? FechaEntregaProgramada);

public record ProblemaReportadoDto(string ProblemaReportado);

public record MarcaDanoCreateDto(decimal X, decimal Y, TipoMarcaDano Tipo, string? Descripcion);

public record MecanicoAsignarDto(Guid EmpleadoId);

public record EncuestaOrdenDto(int Calificacion, string? Comentario);
