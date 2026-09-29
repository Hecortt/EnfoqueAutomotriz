using Asp.Versioning;
using AspNetCoreRateLimit;
using EnfoqueAutomotriz.Api.Endpoints;
using EnfoqueAutomotriz.Api.Extensions;
using EnfoqueAutomotriz.Api.Mapping;
using EnfoqueAutomotriz.Api.Middleware;
using EnfoqueAutomotriz.Api.Context;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

// Npgsql 6+: permitir DateTime con Kind=Local/Unspecified (legacy behavior)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 524_288_000); // 500 MB

if (string.IsNullOrEmpty(builder.Environment.WebRootPath))
    builder.Environment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

// ── Log de diagnóstico al arrancar ────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=================================================");
Console.WriteLine("  EnfoqueAutomotriz API — Plataforma SaaS multi-taller");
Console.WriteLine($"  Entorno       : {builder.Environment.EnvironmentName}");
Console.WriteLine($"  ContentRoot   : {builder.Environment.ContentRootPath}");
Console.WriteLine("=================================================");
Console.ResetColor();

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddDatabase(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddSwaggerWithVersioning();
builder.Services.AddRepositories();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddRecaptcha(builder.Configuration);
builder.Services.AddHttpClient("DeepSeek");

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["https://localhost:7102"];
var corsDomains = builder.Configuration.GetSection("Cors:AllowedDomains").Get<string[]>() ?? [];
builder.Services.AddCors(opt => opt.AddPolicy("AllowUI", p =>
{
    p.SetIsOriginAllowed(origin =>
    {
        if (corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            return true;

        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            foreach (var domain in corsDomains)
            {
                if (uri.Host.EndsWith(domain, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials();
}));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    // Evita errores de serialización por referencias circulares entre entidades EF Core
    // relacionadas (ej. Vehiculo.Cliente <-> Cliente.Vehiculos, Empleado.JefeDirecto <-> Subordinados).
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// ── Test de conexión a BD al arrancar ─────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var canConnect = await db.Database.CanConnectAsync();

    Console.ForegroundColor = canConnect ? ConsoleColor.Green : ConsoleColor.Red;
    Console.WriteLine(canConnect
        ? "  [DB] Conexión a PostgreSQL: OK"
        : "  [DB] ERROR: No se pudo conectar a PostgreSQL");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  [DB] ERROR de conexión: {ex.Message}");
    Console.ResetColor();
}

// ── Pipeline ──────────────────────────────────────────────────────────────────
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles();
app.UseSecurityHeaders();
app.UseIpRateLimiting();
app.UseExceptionMiddleware();
app.UseCors("AllowUI");
app.UseSwaggerWithVersioning();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<FeatureGateMiddleware>();

var v1 = new ApiVersion(1, 0);
var versionSet = app.NewApiVersionSet().HasApiVersion(v1).Build();
var api = app.MapGroup("/api/v{version:apiVersion}").WithApiVersionSet(versionSet);

api.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }))
   .MapToApiVersion(v1).WithTags("Core").AllowAnonymous();

api.MapGet("/health/db", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = canConnect ? "ok" : "error",
            database = db.Database.GetDbConnection().Database,
            server = db.Database.GetDbConnection().DataSource,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.MapToApiVersion(v1).WithTags("Core").AllowAnonymous();

// ── Módulos transversales (fase 1: infraestructura SaaS) ──────────────────────
api.MapAuthEndpoints(v1);
api.MapCatalogsEndpoints(v1);
api.MapFeatureFlagsEndpoints(v1);
api.MapSaasAdminEndpoints(v1);
api.MapNotificacionesEndpoints(v1);
api.MapConfiguracionEmpresaEndpoints(v1);
api.MapTenantInfoEndpoints(v1);

// ── Taller automotriz ──────────────────────────────────────────────────────
api.MapSucursalesEndpoints(v1);
api.MapClientesEndpoints(v1);
api.MapOrdenesServicioEndpoints(v1);
api.MapSeguimientoEndpoints(v1);
api.MapAlmacenEndpoints(v1);
api.MapComprasEndpoints(v1);
api.MapPromocionesEndpoints(v1);
api.MapRHEndpoints(v1);
api.MapComunicacionEndpoints(v1);
api.MapIaEndpoints(v1);

// Los módulos restantes del taller automotriz se agregarán siguiendo el mismo
// patrón: Domain -> Context.Configurations -> Repository -> Endpoints -> Program.cs.

app.Run();
