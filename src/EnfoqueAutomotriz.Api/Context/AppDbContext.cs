using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using EnfoqueAutomotriz.Domain.Auth;
using EnfoqueAutomotriz.Domain.Catalogs;
using EnfoqueAutomotriz.Domain.Configuracion;
using EnfoqueAutomotriz.Domain.Shared;
using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.Domain.Clientes;
using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.Domain.Almacen;
using EnfoqueAutomotriz.Domain.Compras;
using EnfoqueAutomotriz.Domain.Promociones;
using EnfoqueAutomotriz.Domain.Comunicacion;
using EnfoqueAutomotriz.Domain.RH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EnfoqueAutomotriz.Api.Context;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpCtx;
    private readonly ILogger<AppDbContext>? _logger;

    public AppDbContext(DbContextOptions<AppDbContext> options,
        IHttpContextAccessor? httpCtx = null,
        ILogger<AppDbContext>? logger = null)
        : base(options)
    {
        _httpCtx = httpCtx;
        _logger = logger;
    }

    // ── Auth / infraestructura transversal ───────────────────────────────────
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<AccesoLog> AccesosLog => Set<AccesoLog>();
    public DbSet<AuditoriaGeneral> AuditoriaGeneral => Set<AuditoriaGeneral>();
    public DbSet<PermisoRol> PermisosRol => Set<PermisoRol>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    // ── Catálogos genéricos y configuración ──────────────────────────────────
    public DbSet<Catalogo> Catalogos => Set<Catalogo>();
    public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa => Set<ConfiguracionEmpresa>();

    // ── Taller: sucursales, clientes, vehículos, órdenes de servicio ─────────
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<OrdenServicio> OrdenesServicio => Set<OrdenServicio>();
    public DbSet<OrdenServicioConcepto> OrdenesServicioConceptos => Set<OrdenServicioConcepto>();
    public DbSet<OrdenServicioFoto> OrdenesServicioFotos => Set<OrdenServicioFoto>();
    public DbSet<OrdenServicioEstatusHistorial> OrdenesServicioHistorial => Set<OrdenServicioEstatusHistorial>();
    public DbSet<EncuestaSatisfaccion> EncuestasSatisfaccion => Set<EncuestaSatisfaccion>();
    public DbSet<OrdenServicioMarcaDano> OrdenesServicioMarcasDano => Set<OrdenServicioMarcaDano>();
    public DbSet<OrdenServicioMecanico> OrdenesServicioMecanicos => Set<OrdenServicioMecanico>();

    // ── Almacén / Compras ─────────────────────────────────────────────────────
    public DbSet<Refaccion> Refacciones => Set<Refaccion>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<SolicitudAlmacen> SolicitudesAlmacen => Set<SolicitudAlmacen>();
    public DbSet<SolicitudAlmacenDetalle> SolicitudesAlmacenDetalle => Set<SolicitudAlmacenDetalle>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<OrdenCompraDetalle> OrdenesCompraDetalle => Set<OrdenCompraDetalle>();

    // ── Promociones y comunicación ────────────────────────────────────────────
    public DbSet<Promocion> Promociones => Set<Promocion>();
    public DbSet<PlantillaMensaje> PlantillasMensaje => Set<PlantillaMensaje>();
    public DbSet<MensajeEnviado> MensajesEnviados => Set<MensajeEnviado>();

    // ── RH ────────────────────────────────────────────────────────────────────
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Puesto> Puestos => Set<Puesto>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Capacitacion> Capacitaciones => Set<Capacitacion>();
    public DbSet<EmpleadoCapacitacion> EmpleadosCapacitaciones => Set<EmpleadoCapacitacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter: soft-delete para CatalogEntity y BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            bool isCatalog = typeof(CatalogEntity).IsAssignableFrom(clr);
            bool isBase = typeof(BaseEntity).IsAssignableFrom(clr);
            if (!isCatalog && !isBase) continue;

            var isDeletedProp = entityType.FindProperty(nameof(BaseEntity.IsDeleted));
            if (isDeletedProp == null) continue;

            var param = Expression.Parameter(clr, "e");
            var prop = Expression.Property(param, nameof(BaseEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(prop), param);
            modelBuilder.Entity(clr).HasQueryFilter(filter);
        }

        base.OnModelCreating(modelBuilder);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Auditoría automática: captura todos los cambios en SaveChangesAsync
    // ═══════════════════════════════════════════════════════════════════════

    private static readonly HashSet<string> _auditableEntities =
    [
        nameof(Usuario), nameof(ConfiguracionEmpresa), nameof(Catalogo), nameof(PermisoRol),
        nameof(Cliente), nameof(Vehiculo), nameof(OrdenServicio),
        nameof(Refaccion), nameof(OrdenCompra), nameof(Empleado), nameof(Promocion),
        nameof(Capacitacion), nameof(EmpleadoCapacitacion),
    ];

    private static readonly HashSet<string> _ignoredProperties =
    [
        "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "IsDeleted",
    ];

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private bool _isAuditSave;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isAuditSave)
            return await base.SaveChangesAsync(cancellationToken);

        List<AuditEntry>? auditEntries;
        try
        {
            auditEntries = CollectAuditEntries();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Auditoría: error al recolectar cambios");
            auditEntries = null;
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditEntries is { Count: > 0 })
        {
            _isAuditSave = true;
            try
            {
                await PersistAuditEntries(auditEntries, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Auditoría: error al persistir {Count} registros", auditEntries.Count);
                foreach (var e in ChangeTracker.Entries<AuditoriaGeneral>()
                    .Where(e => e.State == EntityState.Added).ToList())
                {
                    e.State = EntityState.Detached;
                }
            }
            finally
            {
                _isAuditSave = false;
            }
        }
        return result;
    }

    private List<AuditEntry> CollectAuditEntries()
    {
        List<AuditEntry> entries = [];

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditoriaGeneral || entry.Entity is AccesoLog)
                continue;

            var entityName = entry.Entity.GetType().Name;
            if (!_auditableEntities.Contains(entityName))
                continue;

            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry
            {
                EntityName = entityName,
                State = entry.State,
            };

            var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            if (idProp != null)
                auditEntry.EntityId = idProp.CurrentValue?.ToString() ?? "";

            var isDeletedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
            if (entry.State == EntityState.Modified && isDeletedProp != null
                && isDeletedProp.IsModified
                && isDeletedProp.CurrentValue is true && isDeletedProp.OriginalValue is false)
            {
                auditEntry.State = EntityState.Deleted;
            }

            foreach (var prop in entry.Properties)
            {
                try
                {
                    if (prop.Metadata.IsShadowProperty()) continue;
                    if (_ignoredProperties.Contains(prop.Metadata.Name)) continue;

                    switch (auditEntry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[prop.Metadata.Name] = ToSerializableValue(prop.CurrentValue);
                            break;
                        case EntityState.Modified when prop.IsModified:
                            auditEntry.OldValues[prop.Metadata.Name] = ToSerializableValue(prop.OriginalValue);
                            auditEntry.NewValues[prop.Metadata.Name] = ToSerializableValue(prop.CurrentValue);
                            break;
                        case EntityState.Deleted:
                            auditEntry.OldValues[prop.Metadata.Name] = ToSerializableValue(prop.OriginalValue);
                            break;
                    }
                }
                catch { /* skip property that can't be read */ }
            }

            if (auditEntry.OldValues.Count == 0 && auditEntry.NewValues.Count == 0)
                continue;

            entries.Add(auditEntry);
        }

        return entries;
    }

    private async Task PersistAuditEntries(List<AuditEntry> auditEntries, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var ip = GetCurrentIP();

        foreach (var ae in auditEntries)
        {
            var audit = new AuditoriaGeneral
            {
                UsuarioId = userId,
                UsuarioEmail = userEmail,
                Entidad = ae.EntityName,
                EntidadId = ae.EntityId,
                Accion = ae.State switch
                {
                    EntityState.Added => AccionAuditoria.Crear,
                    EntityState.Deleted => AccionAuditoria.Eliminar,
                    _ => AccionAuditoria.Modificar
                },
                ValoresAnteriores = ae.OldValues.Count > 0 ? SafeSerialize(ae.OldValues) : null,
                ValoresNuevos = ae.NewValues.Count > 0 ? SafeSerialize(ae.NewValues) : null,
                Fecha = DateTime.UtcNow,
                IP = ip,
                Modulo = ResolveModulo(ae.EntityName)
            };
            AuditoriaGeneral.Add(audit);
        }

        await base.SaveChangesAsync(ct);
    }

    private static object? ToSerializableValue(object? value) => value switch
    {
        null => null,
        string s => s,
        Guid g => g.ToString(),
        DateTime dt => dt.ToString("O"),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        bool or int or long or short or byte or decimal or double or float => value,
        Enum e => e.ToString(),
        _ => value.ToString()
    };

    private static string SafeSerialize(Dictionary<string, object?> dict)
    {
        try { return JsonSerializer.Serialize(dict, _jsonOpts); }
        catch { return JsonSerializer.Serialize(dict.ToDictionary(k => k.Key, k => k.Value?.ToString()), _jsonOpts); }
    }

    private Guid? GetCurrentUserId()
    {
        var sub = _httpCtx?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string? GetCurrentUserEmail() => _httpCtx?.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    private string? GetCurrentIP() => _httpCtx?.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    private static string ResolveModulo(string entityName) => entityName switch
    {
        nameof(Usuario) => "Auth",
        nameof(ConfiguracionEmpresa) => "Empresa",
        nameof(Catalogo) => "Catalogos",
        nameof(PermisoRol) => "Permisos",
        nameof(Cliente) or nameof(Vehiculo) => "Clientes",
        nameof(OrdenServicio) => "OrdenesServicio",
        nameof(Refaccion) => "Almacen",
        nameof(OrdenCompra) => "Compras",
        nameof(Empleado) => "RH",
        nameof(Capacitacion) or nameof(EmpleadoCapacitacion) => "RH",
        nameof(Promocion) => "Promociones",
        _ => "Otro"
    };

    private class AuditEntry
    {
        public string EntityName { get; set; } = "";
        public string EntityId { get; set; } = "";
        public EntityState State { get; set; }
        public Dictionary<string, object?> OldValues { get; } = [];
        public Dictionary<string, object?> NewValues { get; } = [];
    }
}
