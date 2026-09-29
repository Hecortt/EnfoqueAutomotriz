using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Auth;
using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.Domain.RH;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class OrdenServicioRepository : IOrdenServicioRepository
{
    private readonly AppDbContext _db;
    private readonly IClienteRepository _clientes;
    private readonly INotificacionRepository _notificaciones;

    // Roles que reciben notificaciones operativas del taller (AdminTaller, SoporteIT).
    private static readonly int[] _rolesNotificables = [1, 6];

    public OrdenServicioRepository(AppDbContext db, IClienteRepository clientes, INotificacionRepository notificaciones)
    {
        _db = db;
        _clientes = clientes;
        _notificaciones = notificaciones;
    }

    private IQueryable<OrdenServicio> BaseQuery() => _db.OrdenesServicio
        .Include(o => o.Sucursal)
        .Include(o => o.Cliente)
        .Include(o => o.Vehiculo)
        .Include(o => o.Conceptos)
        .Include(o => o.Fotos)
        .Include(o => o.Historial)
        .Include(o => o.MarcasDano);

    public Task<List<OrdenServicio>> GetAllAsync(Guid? sucursalId = null, EstadoOrdenServicio? estado = null, CancellationToken ct = default)
    {
        var query = _db.OrdenesServicio
            .Include(o => o.Sucursal)
            .Include(o => o.Cliente)
            .Include(o => o.Vehiculo)
            .AsQueryable();

        if (sucursalId.HasValue) query = query.Where(o => o.SucursalId == sucursalId.Value);
        if (estado.HasValue) query = query.Where(o => o.Estado == estado.Value);

        return query.OrderByDescending(o => o.FechaRecepcion).ToListAsync(ct);
    }

    public Task<OrdenServicio?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        BaseQuery().FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<OrdenServicio?> GetByTokenAsync(Guid token, CancellationToken ct = default) =>
        BaseQuery().FirstOrDefaultAsync(o => o.TokenSeguimiento == token, ct);

    public async Task<string> GenerarFolioAsync(CancellationToken ct = default)
    {
        var count = await _db.OrdenesServicio.IgnoreQueryFilters().CountAsync(ct);
        return $"OS-{(count + 1):D5}";
    }

    public async Task CreateAsync(OrdenServicio orden, CancellationToken ct = default)
    {
        orden.Historial.Add(new OrdenServicioEstatusHistorial
        {
            OrdenServicioId = orden.Id,
            Estado = orden.Estado,
            Comentario = "Vehículo recibido en taller",
            Fecha = DateTime.UtcNow
        });

        _db.OrdenesServicio.Add(orden);
        await _db.SaveChangesAsync(ct);
        await _clientes.RegistrarVisitaAsync(orden.ClienteId, ct);

        await _notificaciones.CreateParaRolesAsync(_rolesNotificables,
            "Nueva orden recibida",
            $"Se registró la orden {orden.Folio} en el taller.",
            TipoNotificacion.Info,
            $"/OrdenesServicio/Detail/{orden.Id}", ct);
    }

    public async Task UpdateAsync(OrdenServicio orden, CancellationToken ct = default)
    {
        orden.UpdatedAt = DateTime.UtcNow;
        _db.OrdenesServicio.Update(orden);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddConceptoAsync(OrdenServicioConcepto concepto, CancellationToken ct = default)
    {
        concepto.Importe = concepto.Cantidad * concepto.PrecioUnitario;
        _db.OrdenesServicioConceptos.Add(concepto);
        await _db.SaveChangesAsync(ct);
        await RecalcularTotalesAsync(concepto.OrdenServicioId, ct);
    }

    public async Task RemoveConceptoAsync(Guid conceptoId, CancellationToken ct = default)
    {
        var concepto = await _db.OrdenesServicioConceptos.FirstOrDefaultAsync(c => c.Id == conceptoId, ct);
        if (concepto == null) return;
        var ordenId = concepto.OrdenServicioId;
        concepto.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        await RecalcularTotalesAsync(ordenId, ct);
    }

    public async Task RecalcularTotalesAsync(Guid ordenId, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio.Include(o => o.Conceptos).FirstOrDefaultAsync(o => o.Id == ordenId, ct);
        if (orden == null) return;

        var conceptosActivos = orden.Conceptos.Where(c => !c.IsDeleted).ToList();
        var subtotal = conceptosActivos.Sum(c => c.Importe);
        var costoRefacciones = conceptosActivos.Where(c => c.Tipo == TipoConceptoOrden.Refaccion).Sum(c => c.Cantidad * c.CostoUnitario);

        orden.Subtotal = subtotal;
        orden.CostoRefacciones = costoRefacciones;
        var baseGravable = subtotal - orden.Descuento;
        orden.Iva = Math.Round(baseGravable * 0.16m, 2);
        orden.Total = baseGravable + orden.Iva;
        orden.Saldo = orden.Total - orden.Anticipo;

        await _db.SaveChangesAsync(ct);
    }

    public async Task AddFotoAsync(OrdenServicioFoto foto, CancellationToken ct = default)
    {
        _db.OrdenesServicioFotos.Add(foto);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<OrdenServicioMarcaDano> AddMarcaDanoAsync(OrdenServicioMarcaDano marca, CancellationToken ct = default)
    {
        _db.OrdenesServicioMarcasDano.Add(marca);
        await _db.SaveChangesAsync(ct);
        return marca;
    }

    public async Task RemoveMarcaDanoAsync(Guid marcaId, CancellationToken ct = default)
    {
        var marca = await _db.OrdenesServicioMarcasDano.FirstOrDefaultAsync(m => m.Id == marcaId, ct);
        if (marca == null) return;
        _db.OrdenesServicioMarcasDano.Remove(marca);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<object>> GetMecanicosAsync(Guid ordenId, CancellationToken ct = default)
    {
        var asignaciones = await _db.OrdenesServicioMecanicos
            .Where(m => m.OrdenServicioId == ordenId)
            .ToListAsync(ct);

        var empleadoIds = asignaciones.Select(a => a.EmpleadoId).ToList();
        var empleados = await _db.Empleados
            .Include(e => e.Puesto)
            .Where(e => empleadoIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => new { e.Nombres, e.ApellidoPaterno, Puesto = e.Puesto != null ? e.Puesto.Nombre : null }, ct);

        return asignaciones.Select(a => new
        {
            AsignacionId = a.Id,
            EmpleadoId = a.EmpleadoId,
            Nombre = empleados.TryGetValue(a.EmpleadoId, out var emp) ? $"{emp.Nombres} {emp.ApellidoPaterno}" : "(empleado no encontrado)",
            Puesto = empleados.TryGetValue(a.EmpleadoId, out var emp2) ? emp2.Puesto : null
        }).Cast<object>().ToList();
    }

    public async Task<object> AddMecanicoAsync(Guid ordenId, Guid empleadoId, CancellationToken ct = default)
    {
        var yaAsignado = await _db.OrdenesServicioMecanicos.AnyAsync(m => m.OrdenServicioId == ordenId && m.EmpleadoId == empleadoId, ct);
        if (yaAsignado)
            throw new InvalidOperationException("Este mecánico ya está asignado a la orden.");

        var asignacion = new OrdenServicioMecanico { OrdenServicioId = ordenId, EmpleadoId = empleadoId };
        _db.OrdenesServicioMecanicos.Add(asignacion);
        await _db.SaveChangesAsync(ct);

        var empleado = await _db.Empleados.Include(e => e.Puesto).FirstOrDefaultAsync(e => e.Id == empleadoId, ct);
        return new
        {
            AsignacionId = asignacion.Id,
            EmpleadoId = empleadoId,
            Nombre = empleado != null ? $"{empleado.Nombres} {empleado.ApellidoPaterno}" : "(empleado no encontrado)",
            Puesto = empleado?.Puesto?.Nombre
        };
    }

    public async Task RemoveMecanicoAsync(Guid asignacionId, CancellationToken ct = default)
    {
        var asignacion = await _db.OrdenesServicioMecanicos.FirstOrDefaultAsync(m => m.Id == asignacionId, ct);
        if (asignacion == null) return;
        _db.OrdenesServicioMecanicos.Remove(asignacion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<object>> GetMecanicosDisponiblesAsync(Guid? sucursalId = null, CancellationToken ct = default)
    {
        var query = _db.Empleados
            .Include(e => e.Puesto)
            .Where(e => e.Activo && e.Puesto != null &&
                (EF.Functions.ILike(e.Puesto.Nombre, "%mec%") || EF.Functions.ILike(e.Puesto.Nombre, "%especialista%")))
            .AsQueryable();

        if (sucursalId.HasValue) query = query.Where(e => e.SucursalId == sucursalId.Value);

        return await query
            .OrderBy(e => e.Nombres)
            .Select(e => new
            {
                Id = e.Id,
                Nombre = e.Nombres + " " + e.ApellidoPaterno,
                Puesto = e.Puesto!.Nombre,
                SucursalId = e.SucursalId
            })
            .Cast<object>()
            .ToListAsync(ct);
    }

    public async Task SetFechaEntregaProgramadaAsync(Guid ordenId, DateTime? fecha, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio.FirstOrDefaultAsync(o => o.Id == ordenId, ct);
        if (orden == null) return;
        orden.FechaEntregaProgramada = fecha;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateProblemaReportadoAsync(Guid ordenId, string problemaReportado, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio.FirstOrDefaultAsync(o => o.Id == ordenId, ct);
        if (orden == null) return;
        orden.ProblemaReportado = problemaReportado;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CambiarEstadoAsync(Guid ordenId, EstadoOrdenServicio nuevoEstado, string? comentario, Guid? usuarioId, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio.FirstOrDefaultAsync(o => o.Id == ordenId, ct);
        if (orden == null) return;

        orden.Estado = nuevoEstado;
        orden.UpdatedAt = DateTime.UtcNow;
        if (nuevoEstado == EstadoOrdenServicio.Entregado)
            orden.FechaEntrega = DateTime.UtcNow;

        _db.OrdenesServicioHistorial.Add(new OrdenServicioEstatusHistorial
        {
            OrdenServicioId = ordenId,
            Estado = nuevoEstado,
            Comentario = comentario,
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task EnviarCotizacionAsync(Guid ordenId, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio.FirstOrDefaultAsync(o => o.Id == ordenId, ct);
        if (orden == null) return;

        orden.EstadoCotizacion = EstadoCotizacion.Pendiente;
        orden.Estado = EstadoOrdenServicio.EsperandoAprobacion;
        _db.OrdenesServicioHistorial.Add(new OrdenServicioEstatusHistorial
        {
            OrdenServicioId = ordenId,
            Estado = EstadoOrdenServicio.EsperandoAprobacion,
            Comentario = "Cotización enviada al cliente para su aprobación",
            Fecha = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> AprobarCotizacionAsync(Guid token, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio.FirstOrDefaultAsync(o => o.TokenSeguimiento == token, ct);
        if (orden == null || orden.EstadoCotizacion != EstadoCotizacion.Pendiente) return false;

        orden.EstadoCotizacion = EstadoCotizacion.Aprobada;
        orden.FechaAprobacionCotizacion = DateTime.UtcNow;
        orden.Estado = EstadoOrdenServicio.EnReparacion;

        _db.OrdenesServicioHistorial.Add(new OrdenServicioEstatusHistorial
        {
            OrdenServicioId = orden.Id,
            Estado = EstadoOrdenServicio.EnReparacion,
            Comentario = "Cliente aprobó la cotización desde el portal de seguimiento",
            Fecha = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        await _notificaciones.CreateParaRolesAsync(_rolesNotificables,
            "Cotización aprobada",
            $"El cliente aprobó la cotización de la orden {orden.Folio}. Ya se puede iniciar la reparación.",
            TipoNotificacion.Success,
            $"/OrdenesServicio/Detail/{orden.Id}", ct);

        return true;
    }

    public async Task<bool> RechazarCotizacionAsync(Guid token, string? comentario, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio.FirstOrDefaultAsync(o => o.TokenSeguimiento == token, ct);
        if (orden == null || orden.EstadoCotizacion != EstadoCotizacion.Pendiente) return false;

        orden.EstadoCotizacion = EstadoCotizacion.Rechazada;
        orden.ComentarioRechazo = comentario;

        _db.OrdenesServicioHistorial.Add(new OrdenServicioEstatusHistorial
        {
            OrdenServicioId = orden.Id,
            Estado = orden.Estado,
            Comentario = $"Cliente rechazó la cotización: {comentario}",
            Fecha = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        await _notificaciones.CreateParaRolesAsync(_rolesNotificables,
            "Cotización rechazada",
            $"El cliente rechazó la cotización de la orden {orden.Folio}." + (string.IsNullOrWhiteSpace(comentario) ? "" : $" Motivo: {comentario}"),
            TipoNotificacion.Warning,
            $"/OrdenesServicio/Detail/{orden.Id}", ct);

        return true;
    }

    public async Task RegistrarEncuestaAsync(Guid ordenId, int calificacion, string? comentario, CancellationToken ct = default)
    {
        var existe = await _db.EncuestasSatisfaccion.AnyAsync(e => e.OrdenServicioId == ordenId, ct);
        if (existe) return;

        _db.EncuestasSatisfaccion.Add(new EncuestaSatisfaccion
        {
            OrdenServicioId = ordenId,
            Calificacion = Math.Clamp(calificacion, 1, 5),
            Comentario = comentario
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<object> GetDashboardKpisAsync(Guid? sucursalId = null, CancellationToken ct = default)
    {
        var query = _db.OrdenesServicio.AsQueryable();
        if (sucursalId.HasValue) query = query.Where(o => o.SucursalId == sucursalId.Value);

        var activas = await query.CountAsync(o => o.Estado != EstadoOrdenServicio.Entregado && o.Estado != EstadoOrdenServicio.Cancelado, ct);

        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var delMes = await query.Where(o => o.FechaRecepcion >= inicioMes && o.Estado != EstadoOrdenServicio.Cancelado).ToListAsync(ct);

        var ingresosMes = delMes.Sum(o => o.Total);
        var costoMes = delMes.Sum(o => o.CostoRefacciones);
        var margenMes = ingresosMes - costoMes;
        var margenPorcentaje = ingresosMes > 0 ? Math.Round((margenMes / ingresosMes) * 100, 1) : 0;

        var porSucursal = await _db.OrdenesServicio
            .Include(o => o.Sucursal)
            .Where(o => o.FechaRecepcion >= inicioMes && o.Estado != EstadoOrdenServicio.Cancelado)
            .GroupBy(o => new { o.SucursalId, o.Sucursal!.Nombre })
            .Select(g => new
            {
                SucursalId = g.Key.SucursalId,
                Sucursal = g.Key.Nombre,
                Ordenes = g.Count(),
                Ingresos = g.Sum(o => o.Total),
                Margen = g.Sum(o => o.Total - o.CostoRefacciones)
            })
            .ToListAsync(ct);

        var clientesRecurrentes = await _db.Clientes.CountAsync(c => c.NumeroVisitas > 1, ct);
        var clientesTotal = await _db.Clientes.CountAsync(ct);

        return new
        {
            OrdenesActivas = activas,
            IngresosMes = ingresosMes,
            MargenMes = margenMes,
            MargenPorcentaje = margenPorcentaje,
            PorSucursal = porSucursal,
            ClientesRecurrentes = clientesRecurrentes,
            ClientesTotal = clientesTotal
        };
    }

    /// <summary>Dashboard consolidado de toda la empresa (todas las sucursales).</summary>
    public async Task<object> GetDashboardEmpresaAsync(CancellationToken ct = default)
    {
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activas = await _db.OrdenesServicio.CountAsync(o => o.Estado != EstadoOrdenServicio.Entregado && o.Estado != EstadoOrdenServicio.Cancelado, ct);
        var delMes = await _db.OrdenesServicio.Where(o => o.FechaRecepcion >= inicioMes && o.Estado != EstadoOrdenServicio.Cancelado).ToListAsync(ct);

        var ingresosMes = delMes.Sum(o => o.Total);
        var costoMes = delMes.Sum(o => o.CostoRefacciones);
        var margenMes = ingresosMes - costoMes;
        var margenPorcentaje = ingresosMes > 0 ? Math.Round((margenMes / ingresosMes) * 100, 1) : 0;

        var porSucursal = await _db.OrdenesServicio
            .Include(o => o.Sucursal)
            .Where(o => o.FechaRecepcion >= inicioMes && o.Estado != EstadoOrdenServicio.Cancelado)
            .GroupBy(o => new { o.SucursalId, o.Sucursal!.Nombre })
            .Select(g => new
            {
                SucursalId = g.Key.SucursalId,
                Sucursal = g.Key.Nombre,
                Ordenes = g.Count(),
                Ingresos = g.Sum(o => o.Total),
                Margen = g.Sum(o => o.Total - o.CostoRefacciones)
            })
            .OrderByDescending(s => s.Ingresos)
            .ToListAsync(ct);

        var clientesRecurrentes = await _db.Clientes.CountAsync(c => c.NumeroVisitas > 1, ct);
        var clientesTotal = await _db.Clientes.CountAsync(ct);
        var sucursalesTotal = await _db.Sucursales.CountAsync(s => s.Activa, ct);
        var empleadosTotal = await _db.Empleados.CountAsync(e => e.Activo, ct);

        // Tendencia de los últimos 6 meses (ingresos y margen)
        var inicioTendencia = inicioMes.AddMonths(-5);
        var ordenesTendencia = await _db.OrdenesServicio
            .Where(o => o.FechaRecepcion >= inicioTendencia && o.Estado != EstadoOrdenServicio.Cancelado)
            .Select(o => new { o.FechaRecepcion, o.Total, o.CostoRefacciones })
            .ToListAsync(ct);

        var tendencia = Enumerable.Range(0, 6)
            .Select(i => inicioMes.AddMonths(-5 + i))
            .Select(mes =>
            {
                var delMesTendencia = ordenesTendencia.Where(o => o.FechaRecepcion.Year == mes.Year && o.FechaRecepcion.Month == mes.Month).ToList();
                return new
                {
                    Mes = mes.ToString("MMM yyyy", new System.Globalization.CultureInfo("es-MX")),
                    Ingresos = delMesTendencia.Sum(o => o.Total),
                    Margen = delMesTendencia.Sum(o => o.Total - o.CostoRefacciones)
                };
            })
            .ToList();

        // Distribución de órdenes activas por estado (para gráfica de dona)
        var porEstado = await _db.OrdenesServicio
            .Where(o => o.Estado != EstadoOrdenServicio.Cancelado)
            .GroupBy(o => o.Estado)
            .Select(g => new { Estado = g.Key.ToString(), Cantidad = g.Count() })
            .ToListAsync(ct);

        // KPIs de tiempos de entrega (calidad/rapidez del servicio)
        var entregadas = await _db.OrdenesServicio
            .Where(o => o.Estado == EstadoOrdenServicio.Entregado && o.FechaEntrega != null)
            .Select(o => new { o.FechaRecepcion, o.FechaEntrega, o.FechaEntregaProgramada })
            .ToListAsync(ct);

        var conProgramada = entregadas.Where(o => o.FechaEntregaProgramada != null).ToList();
        var entregasATiempo = conProgramada.Count(o => o.FechaEntrega!.Value <= o.FechaEntregaProgramada!.Value);
        var porcentajeATiempo = conProgramada.Count > 0 ? Math.Round((decimal)entregasATiempo / conProgramada.Count * 100, 1) : (decimal?)null;
        var tiempoPromedioHoras = entregadas.Count > 0
            ? Math.Round(entregadas.Average(o => (o.FechaEntrega!.Value - o.FechaRecepcion).TotalHours), 1)
            : (double?)null;

        return new
        {
            SucursalesTotal = sucursalesTotal,
            EmpleadosTotal = empleadosTotal,
            OrdenesActivas = activas,
            IngresosMes = ingresosMes,
            MargenMes = margenMes,
            MargenPorcentaje = margenPorcentaje,
            PorSucursal = porSucursal,
            ClientesRecurrentes = clientesRecurrentes,
            ClientesTotal = clientesTotal,
            Tendencia = tendencia,
            PorEstado = porEstado,
            EntregasATiempoPorcentaje = porcentajeATiempo,
            TiempoPromedioReparacionHoras = tiempoPromedioHoras
        };
    }

    /// <summary>Dashboard operativo de una sucursal específica (taller).</summary>
    public async Task<object?> GetDashboardSucursalAsync(Guid sucursalId, CancellationToken ct = default)
    {
        var sucursal = await _db.Sucursales.FirstOrDefaultAsync(s => s.Id == sucursalId, ct);
        if (sucursal == null) return null;

        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var queryBase = _db.OrdenesServicio.Where(o => o.SucursalId == sucursalId);

        var activas = await queryBase.CountAsync(o => o.Estado != EstadoOrdenServicio.Entregado && o.Estado != EstadoOrdenServicio.Cancelado, ct);
        var delMes = await queryBase.Where(o => o.FechaRecepcion >= inicioMes && o.Estado != EstadoOrdenServicio.Cancelado).ToListAsync(ct);

        var ingresosMes = delMes.Sum(o => o.Total);
        var costoMes = delMes.Sum(o => o.CostoRefacciones);
        var margenMes = ingresosMes - costoMes;
        var margenPorcentaje = ingresosMes > 0 ? Math.Round((margenMes / ingresosMes) * 100, 1) : 0;

        var clientesAtendidos = await queryBase.Select(o => o.ClienteId).Distinct().CountAsync(ct);
        var empleadosSucursal = await _db.Empleados.CountAsync(e => e.SucursalId == sucursalId && e.Activo, ct);

        var porEstado = await queryBase
            .Where(o => o.Estado != EstadoOrdenServicio.Cancelado)
            .GroupBy(o => o.Estado)
            .Select(g => new { Estado = g.Key.ToString(), Cantidad = g.Count() })
            .ToListAsync(ct);

        // Tendencia 6 meses de esta sucursal
        var inicioTendencia = inicioMes.AddMonths(-5);
        var ordenesTendencia = await queryBase
            .Where(o => o.FechaRecepcion >= inicioTendencia && o.Estado != EstadoOrdenServicio.Cancelado)
            .Select(o => new { o.FechaRecepcion, o.Total, o.CostoRefacciones })
            .ToListAsync(ct);

        var tendencia = Enumerable.Range(0, 6)
            .Select(i => inicioMes.AddMonths(-5 + i))
            .Select(mes =>
            {
                var delMesTendencia = ordenesTendencia.Where(o => o.FechaRecepcion.Year == mes.Year && o.FechaRecepcion.Month == mes.Month).ToList();
                return new
                {
                    Mes = mes.ToString("MMM yyyy", new System.Globalization.CultureInfo("es-MX")),
                    Ingresos = delMesTendencia.Sum(o => o.Total),
                    Margen = delMesTendencia.Sum(o => o.Total - o.CostoRefacciones)
                };
            })
            .ToList();

        // Top técnicos por número de órdenes atendidas (histórico completo de la sucursal)
        var topTecnicos = await queryBase
            .Where(o => o.EmpleadoAsignadoId != null)
            .GroupBy(o => o.EmpleadoAsignadoId)
            .Select(g => new { EmpleadoId = g.Key, Ordenes = g.Count() })
            .OrderByDescending(x => x.Ordenes)
            .Take(5)
            .ToListAsync(ct);

        var empleadoIds = topTecnicos.Where(t => t.EmpleadoId.HasValue).Select(t => t.EmpleadoId!.Value).ToList();
        var empleadosNombres = await _db.Empleados
            .Where(e => empleadoIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => $"{e.Nombres} {e.ApellidoPaterno}", ct);

        var topTecnicosResult = topTecnicos
            .Where(t => t.EmpleadoId.HasValue && empleadosNombres.ContainsKey(t.EmpleadoId!.Value))
            .Select(t => new { Nombre = empleadosNombres[t.EmpleadoId!.Value], Ordenes = t.Ordenes })
            .ToList();

        // Refacciones más usadas en esta sucursal
        var refaccionesMasUsadas = await _db.OrdenesServicioConceptos
            .Where(c => c.RefaccionId != null && c.OrdenServicio!.SucursalId == sucursalId && !c.IsDeleted)
            .GroupBy(c => c.Descripcion)
            .Select(g => new { Refaccion = g.Key, VecesUsada = g.Sum(c => (int)c.Cantidad) })
            .OrderByDescending(x => x.VecesUsada)
            .Take(5)
            .ToListAsync(ct);

        // KPIs de tiempos de entrega de esta sucursal
        var entregadas = await queryBase
            .Where(o => o.Estado == EstadoOrdenServicio.Entregado && o.FechaEntrega != null)
            .Select(o => new { o.FechaRecepcion, o.FechaEntrega, o.FechaEntregaProgramada })
            .ToListAsync(ct);

        var conProgramada = entregadas.Where(o => o.FechaEntregaProgramada != null).ToList();
        var entregasATiempo = conProgramada.Count(o => o.FechaEntrega!.Value <= o.FechaEntregaProgramada!.Value);
        var porcentajeATiempo = conProgramada.Count > 0 ? Math.Round((decimal)entregasATiempo / conProgramada.Count * 100, 1) : (decimal?)null;
        var tiempoPromedioHoras = entregadas.Count > 0
            ? Math.Round(entregadas.Average(o => (o.FechaEntrega!.Value - o.FechaRecepcion).TotalHours), 1)
            : (double?)null;

        return new
        {
            Sucursal = sucursal.Nombre,
            SucursalId = sucursal.Id,
            OrdenesActivas = activas,
            IngresosMes = ingresosMes,
            MargenMes = margenMes,
            MargenPorcentaje = margenPorcentaje,
            ClientesAtendidos = clientesAtendidos,
            EmpleadosSucursal = empleadosSucursal,
            PorEstado = porEstado,
            Tendencia = tendencia,
            TopTecnicos = topTecnicosResult,
            RefaccionesMasUsadas = refaccionesMasUsadas,
            EntregasATiempoPorcentaje = porcentajeATiempo,
            TiempoPromedioReparacionHoras = tiempoPromedioHoras
        };
    }

    /// <summary>Dashboard de calidad: detalle de entregas a tiempo vs. retrasadas, para auditar cumplimiento de compromisos.</summary>
    public async Task<object> GetDashboardCalidadAsync(Guid? sucursalId = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default)
    {
        var query = _db.OrdenesServicio
            .Include(o => o.Sucursal)
            .Include(o => o.Cliente)
            .Include(o => o.Vehiculo)
            .Where(o => o.Estado == EstadoOrdenServicio.Entregado && o.FechaEntrega != null)
            .AsQueryable();

        if (sucursalId.HasValue) query = query.Where(o => o.SucursalId == sucursalId.Value);
        if (desde.HasValue) query = query.Where(o => o.FechaEntrega >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.FechaEntrega <= hasta.Value);

        var entregadas = await query
            .OrderByDescending(o => o.FechaEntrega)
            .Select(o => new
            {
                o.Id,
                o.Folio,
                Sucursal = o.Sucursal!.Nombre,
                Cliente = o.Cliente!.Nombre,
                Vehiculo = o.Vehiculo!.Marca + " " + o.Vehiculo!.Modelo + " (" + o.Vehiculo!.Placas + ")",
                o.FechaRecepcion,
                o.FechaEntregaProgramada,
                o.FechaEntrega,
                o.EmpleadoAsignadoId
            })
            .ToListAsync(ct);

        var resultado = entregadas.Select(o =>
        {
            bool? aTiempo = o.FechaEntregaProgramada.HasValue ? o.FechaEntrega!.Value <= o.FechaEntregaProgramada.Value : (bool?)null;
            double? diferenciaHoras = o.FechaEntregaProgramada.HasValue
                ? Math.Round((o.FechaEntrega!.Value - o.FechaEntregaProgramada.Value).TotalHours, 1)
                : (double?)null;
            double duracionHoras = Math.Round((o.FechaEntrega!.Value - o.FechaRecepcion).TotalHours, 1);

            return new
            {
                o.Id,
                o.Folio,
                o.Sucursal,
                o.Cliente,
                o.Vehiculo,
                o.FechaRecepcion,
                o.FechaEntregaProgramada,
                FechaEntrega = o.FechaEntrega!.Value,
                DuracionHoras = duracionHoras,
                DiferenciaHoras = diferenciaHoras,
                EntregadoATiempo = aTiempo
            };
        }).ToList();

        var conProgramada = resultado.Where(r => r.EntregadoATiempo.HasValue).ToList();
        var aTiempoCount = conProgramada.Count(r => r.EntregadoATiempo == true);
        var retrasadasCount = conProgramada.Count(r => r.EntregadoATiempo == false);
        var sinFecha = resultado.Count - conProgramada.Count;

        var porcentajeATiempo = conProgramada.Count > 0 ? Math.Round((decimal)aTiempoCount / conProgramada.Count * 100, 1) : (decimal?)null;
        var promedioRetrasoHoras = retrasadasCount > 0
            ? Math.Round(conProgramada.Where(r => r.EntregadoATiempo == false).Average(r => r.DiferenciaHoras!.Value), 1)
            : (double?)null;
        var tiempoPromedioHoras = resultado.Count > 0 ? Math.Round(resultado.Average(r => r.DuracionHoras), 1) : (double?)null;

        // ── Tendencia semanal de cumplimiento (últimas 10 semanas con datos) ──
        var tendenciaSemanal = conProgramada
            .GroupBy(r => System.Globalization.ISOWeek.GetWeekOfYear(r.FechaEntrega))
            .Select(g => new
            {
                Semana = $"Sem. {g.Key}",
                InicioSemana = g.Min(x => x.FechaEntrega),
                Total = g.Count(),
                ATiempo = g.Count(x => x.EntregadoATiempo == true),
                Porcentaje = Math.Round((decimal)g.Count(x => x.EntregadoATiempo == true) / g.Count() * 100, 1)
            })
            .OrderBy(g => g.InicioSemana)
            .TakeLast(10)
            .Select(g => new { g.Semana, g.Total, g.ATiempo, g.Porcentaje })
            .ToList();

        // ── Cumplimiento por sucursal (comparativo entre talleres) ────────────
        var porSucursalCalidad = conProgramada
            .GroupBy(r => r.Sucursal)
            .Select(g => new
            {
                Sucursal = g.Key,
                Total = g.Count(),
                ATiempo = g.Count(x => x.EntregadoATiempo == true),
                Retrasadas = g.Count(x => x.EntregadoATiempo == false),
                Porcentaje = Math.Round((decimal)g.Count(x => x.EntregadoATiempo == true) / g.Count() * 100, 1)
            })
            .OrderByDescending(g => g.Porcentaje)
            .ToList();

        // ── Distribución de la magnitud del retraso (histograma) ─────────────
        var retrasadasList = conProgramada.Where(r => r.EntregadoATiempo == false).ToList();
        var histogramaRetraso = new[]
        {
            new { Rango = "≤ 6 hrs", Cantidad = retrasadasList.Count(r => r.DiferenciaHoras!.Value <= 6) },
            new { Rango = "6-24 hrs", Cantidad = retrasadasList.Count(r => r.DiferenciaHoras!.Value > 6 && r.DiferenciaHoras.Value <= 24) },
            new { Rango = "1-3 días", Cantidad = retrasadasList.Count(r => r.DiferenciaHoras!.Value > 24 && r.DiferenciaHoras.Value <= 72) },
            new { Rango = "> 3 días", Cantidad = retrasadasList.Count(r => r.DiferenciaHoras!.Value > 72) }
        };

        // ── Ranking de mecánicos: quién participó en más órdenes retrasadas ──
        // Una orden puede haber sido atendida por un equipo de mecánicos (tabla puente
        // ordenes_servicio_mecanicos); si no se registró equipo, se usa el mecánico
        // único asignado (empleado_asignado_id) como respaldo histórico.
        var ordenIds = entregadas.Select(o => o.Id).ToList();
        var asignacionesEquipo = await _db.OrdenesServicioMecanicos
            .Where(m => ordenIds.Contains(m.OrdenServicioId))
            .ToListAsync(ct);

        var equipoPorOrden = asignacionesEquipo
            .GroupBy(m => m.OrdenServicioId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.EmpleadoId).ToList());

        var resultadoPorId = resultado.ToDictionary(r => r.Id);
        var empleadoAOrdenes = new Dictionary<Guid, List<Guid>>();
        foreach (var o in entregadas)
        {
            var mecanicos = equipoPorOrden.TryGetValue(o.Id, out var lista) && lista.Count > 0
                ? lista
                : (o.EmpleadoAsignadoId.HasValue ? [o.EmpleadoAsignadoId.Value] : []);

            foreach (var empId in mecanicos)
            {
                if (!empleadoAOrdenes.TryGetValue(empId, out var ordenesEmp))
                {
                    ordenesEmp = [];
                    empleadoAOrdenes[empId] = ordenesEmp;
                }
                ordenesEmp.Add(o.Id);
            }
        }

        var empleadosInfo = await _db.Empleados
            .Where(e => empleadoAOrdenes.Keys.Contains(e.Id))
            .Select(e => new { e.Id, Nombre = e.Nombres + " " + e.ApellidoPaterno })
            .ToDictionaryAsync(e => e.Id, e => e.Nombre, ct);

        var rankingMecanicos = empleadoAOrdenes.Select(kv =>
        {
            var ordenesDelEmpleado = kv.Value.Select(id => resultadoPorId[id]).Where(r => r.EntregadoATiempo.HasValue).ToList();
            var total = ordenesDelEmpleado.Count;
            var retrasos = ordenesDelEmpleado.Count(r => r.EntregadoATiempo == false);
            var porcentajeRetraso = total > 0 ? Math.Round((decimal)retrasos / total * 100, 1) : 0;
            var promedioRetrasoEmp = retrasos > 0
                ? Math.Round(ordenesDelEmpleado.Where(r => r.EntregadoATiempo == false).Average(r => r.DiferenciaHoras!.Value), 1)
                : (double?)null;

            return new
            {
                EmpleadoId = kv.Key,
                Nombre = empleadosInfo.TryGetValue(kv.Key, out var n) ? n : "(empleado no encontrado)",
                TotalOrdenes = total,
                Retrasos = retrasos,
                PorcentajeRetraso = porcentajeRetraso,
                PromedioRetrasoHoras = promedioRetrasoEmp
            };
        })
        .Where(x => x.TotalOrdenes > 0)
        .OrderByDescending(x => x.Retrasos)
        .ThenByDescending(x => x.PorcentajeRetraso)
        .ToList();

        return new
        {
            Resumen = new
            {
                Total = resultado.Count,
                ATiempo = aTiempoCount,
                Retrasadas = retrasadasCount,
                SinFechaProgramada = sinFecha,
                PorcentajeATiempo = porcentajeATiempo,
                PromedioRetrasoHoras = promedioRetrasoHoras,
                TiempoPromedioReparacionHoras = tiempoPromedioHoras
            },
            Entregas = resultado,
            TendenciaSemanal = tendenciaSemanal,
            PorSucursal = porSucursalCalidad,
            HistogramaRetraso = histogramaRetraso,
            RankingMecanicos = rankingMecanicos
        };
    }

    /// <summary>
    /// Ficha de desempeño de un técnico/mecánico/jefe de taller: calcula 8 dimensiones
    /// (0-100) a partir de datos reales de órdenes, encuestas, conceptos y capacitación,
    /// y las compara contra el promedio del resto del equipo técnico para identificar
    /// fortalezas y áreas de mejora.
    /// </summary>
    public async Task<object?> GetDesempenoTecnicoAsync(Guid empleadoId, CancellationToken ct = default)
    {
        var empleado = await _db.Empleados.Include(e => e.Puesto).Include(e => e.Sucursal)
            .FirstOrDefaultAsync(e => e.Id == empleadoId, ct);
        if (empleado == null) return null;

        // ── Datos base: todas las órdenes con la info necesaria ───────────────
        var ordenes = await _db.OrdenesServicio
            .Select(o => new
            {
                o.Id,
                o.EmpleadoAsignadoId,
                o.ClienteId,
                o.VehiculoId,
                o.Estado,
                o.FechaRecepcion,
                o.FechaEntrega,
                o.FechaEntregaProgramada
            })
            .ToListAsync(ct);

        var asignacionesEquipo = await _db.OrdenesServicioMecanicos.ToListAsync(ct);
        var equipoPorOrden = asignacionesEquipo.GroupBy(m => m.OrdenServicioId).ToDictionary(g => g.Key, g => g.Select(m => m.EmpleadoId).ToList());

        // Mapa empleado -> lista de órdenes que atendió (equipo si existe, si no el asignado único)
        var empleadoAOrdenes = new Dictionary<Guid, List<Guid>>();
        foreach (var o in ordenes)
        {
            var mecanicos = equipoPorOrden.TryGetValue(o.Id, out var lista) && lista.Count > 0
                ? lista
                : (o.EmpleadoAsignadoId.HasValue ? [o.EmpleadoAsignadoId.Value] : []);

            foreach (var empId in mecanicos)
            {
                if (!empleadoAOrdenes.TryGetValue(empId, out var ordenesEmp)) { ordenesEmp = []; empleadoAOrdenes[empId] = ordenesEmp; }
                ordenesEmp.Add(o.Id);
            }
        }

        if (!empleadoAOrdenes.ContainsKey(empleadoId))
            empleadoAOrdenes[empleadoId] = []; // el técnico existe pero aún no tiene órdenes registradas

        var ordenesPorId = ordenes.ToDictionary(o => o.Id);

        // Encuestas de satisfacción por orden
        var encuestas = await _db.EncuestasSatisfaccion.Select(e => new { e.OrdenServicioId, e.Calificacion }).ToListAsync(ct);
        var encuestaPorOrden = encuestas.ToDictionary(e => e.OrdenServicioId, e => e.Calificacion);

        // Especialidades trabajadas por orden (conceptos de mano de obra con especialidad asignada)
        var conceptos = await _db.OrdenesServicioConceptos
            .Where(c => c.Tipo == TipoConceptoOrden.ManoObra && c.EspecialidadId != null && !c.IsDeleted)
            .Select(c => new { c.OrdenServicioId, c.EspecialidadId })
            .ToListAsync(ct);
        var especialidadesPorOrden = conceptos.GroupBy(c => c.OrdenServicioId).ToDictionary(g => g.Key, g => g.Select(c => c.EspecialidadId!.Value).Distinct().ToList());

        var totalEspecialidadesActivas = await _db.Catalogos.CountAsync(c => c.Tipo == "especialidades" && c.Activo, ct);

        // Capacitación por empleado
        var capacitaciones = await _db.EmpleadosCapacitaciones.Select(a => new { a.EmpleadoId, a.Estado }).ToListAsync(ct);
        var capacitacionesPorEmpleado = capacitaciones.GroupBy(a => a.EmpleadoId).ToDictionary(g => g.Key, g => g.ToList());

        // Vehículos que regresaron dentro de 15 días de una entrega (retrabajo): fechas de recepción por vehículo
        var recepcionesPorVehiculo = ordenes
            .GroupBy(o => o.VehiculoId)
            .ToDictionary(g => g.Key, g => g.Select(o => o.FechaRecepcion).ToList());

        var maxOrdenes = empleadoAOrdenes.Values.Select(v => v.Count).DefaultIfEmpty(0).Max();
        if (maxOrdenes == 0) maxOrdenes = 1;

        object CalcularMetricas(Guid empId, List<Guid> ordenIds)
        {
            var ordenesEmp = ordenIds.Select(id => ordenesPorId[id]).ToList();
            var entregadas = ordenesEmp.Where(o => o.Estado == EstadoOrdenServicio.Entregado && o.FechaEntrega != null).ToList();
            var canceladas = ordenesEmp.Count(o => o.Estado == EstadoOrdenServicio.Cancelado);

            // Puntualidad
            var conProgramada = entregadas.Where(o => o.FechaEntregaProgramada != null).ToList();
            decimal? puntualidad = conProgramada.Count > 0
                ? Math.Round(100m - (decimal)conProgramada.Count(o => o.FechaEntrega!.Value > o.FechaEntregaProgramada!.Value) / conProgramada.Count * 100, 1)
                : null;

            // Velocidad (comparado contra el promedio general de duración)
            var duraciones = entregadas.Select(o => (o.FechaEntrega!.Value - o.FechaRecepcion).TotalHours).ToList();
            double? duracionPromedioEmp = duraciones.Count > 0 ? duraciones.Average() : null;

            // Satisfacción
            var calificaciones = ordenIds.Where(id => encuestaPorOrden.ContainsKey(id)).Select(id => encuestaPorOrden[id]).ToList();
            decimal? satisfaccion = calificaciones.Count > 0
                ? Math.Round((decimal)(calificaciones.Average() - 1) / 4 * 100, 1)
                : null;

            // Productividad
            var productividad = Math.Round((decimal)ordenIds.Count / maxOrdenes * 100, 1);

            // Retrabajos: de sus entregas, ¿el mismo vehículo regresó (cualquier orden) dentro de 15 días?
            int retrabajos = 0;
            foreach (var e in entregadas)
            {
                if (!recepcionesPorVehiculo.TryGetValue(e.VehiculoId, out var recepciones)) continue;
                var regreso = recepciones.Any(f => f > e.FechaEntrega!.Value && (f - e.FechaEntrega!.Value).TotalDays <= 15);
                if (regreso) retrabajos++;
            }
            decimal? retrabajosScore = entregadas.Count > 0 ? Math.Round(100m - (decimal)retrabajos / entregadas.Count * 100, 1) : null;

            // Calidad: finalización exitosa
            var totalCerradas = entregadas.Count + canceladas;
            decimal? calidad = totalCerradas > 0 ? Math.Round((decimal)entregadas.Count / totalCerradas * 100, 1) : null;

            // Especialidades: versatilidad
            var especialidadesDistintas = ordenIds.Where(id => especialidadesPorOrden.ContainsKey(id)).SelectMany(id => especialidadesPorOrden[id]).Distinct().Count();
            decimal? especialidadesScore = totalEspecialidadesActivas > 0
                ? Math.Round(Math.Min(100m, (decimal)especialidadesDistintas / totalEspecialidadesActivas * 100), 1)
                : null;

            // Capacitación
            decimal? capacitacionScore = null;
            int cursosCompletados = 0, cursosTotales = 0;
            if (capacitacionesPorEmpleado.TryGetValue(empId, out var cursosEmp))
            {
                cursosTotales = cursosEmp.Count;
                cursosCompletados = cursosEmp.Count(c => c.Estado == EstadoCapacitacionEmpleado.Completada);
                capacitacionScore = cursosTotales > 0 ? Math.Round((decimal)cursosCompletados / cursosTotales * 100, 1) : null;
            }

            return new
            {
                EmpleadoId = empId,
                Puntualidad = puntualidad,
                Velocidad = duracionPromedioEmp,
                Satisfaccion = satisfaccion,
                Productividad = (decimal?)productividad,
                Retrabajos = retrabajosScore,
                Calidad = calidad,
                Especialidades = especialidadesScore,
                Capacitacion = capacitacionScore,
                // datos crudos para la tabla de transparencia
                TotalOrdenes = ordenIds.Count,
                Entregadas = entregadas.Count,
                Canceladas = canceladas,
                RetrabajosCantidad = retrabajos,
                EspecialidadesDistintas = especialidadesDistintas,
                CursosCompletados = cursosCompletados,
                CursosTotales = cursosTotales,
                DuracionPromedioHoras = duracionPromedioEmp.HasValue ? Math.Round(duracionPromedioEmp.Value, 1) : (double?)null
            };
        }

        var metricasPorEmpleado = empleadoAOrdenes.ToDictionary(kv => kv.Key, kv => CalcularMetricas(kv.Key, kv.Value));

        // Ahora que tenemos duración promedio de todos, calculamos el score de Velocidad relativo
        var duracionesValidas = metricasPorEmpleado.Values
            .Select(m => (double?)m.GetType().GetProperty("Velocidad")!.GetValue(m))
            .Where(v => v.HasValue).Select(v => v!.Value).ToList();
        var duracionPromedioGeneral = duracionesValidas.Count > 0 ? duracionesValidas.Average() : (double?)null;

        decimal? VelocidadScore(double? duracionEmpleado) =>
            duracionEmpleado.HasValue && duracionPromedioGeneral.HasValue && duracionEmpleado.Value > 0
                ? Math.Round((decimal)Math.Min(100, duracionPromedioGeneral.Value / duracionEmpleado.Value * 100), 1)
                : null;

        // Promedio del equipo (excluyendo nulls por eje) para la serie comparativa del radar
        decimal? Promedio(Func<dynamic, object?> selector)
        {
            var valores = metricasPorEmpleado.Values.Select(m => selector(m)).Where(v => v != null).Select(v => Convert.ToDecimal(v)).ToList();
            return valores.Count > 0 ? Math.Round(valores.Average(), 1) : null;
        }

        var miMetrica = (dynamic)metricasPorEmpleado[empleadoId];
        var miVelocidad = VelocidadScore((double?)miMetrica.GetType().GetProperty("Velocidad")!.GetValue(miMetrica));

        var equipoPromedio = new
        {
            Puntualidad = Promedio(m => m.Puntualidad),
            Velocidad = (decimal?)(duracionesValidas.Count > 0 ? 100m : (decimal?)null), // el equipo consigo mismo promedia 100 por definición
            Satisfaccion = Promedio(m => m.Satisfaccion),
            Productividad = Promedio(m => m.Productividad),
            Retrabajos = Promedio(m => m.Retrabajos),
            Calidad = Promedio(m => m.Calidad),
            Especialidades = Promedio(m => m.Especialidades),
            Capacitacion = Promedio(m => m.Capacitacion)
        };

        return new
        {
            Empleado = new
            {
                empleado.Id,
                Nombre = empleado.NombreCompleto,
                Puesto = empleado.Puesto?.Nombre,
                Sucursal = empleado.Sucursal?.Nombre
            },
            Ejes = new
            {
                Puntualidad = miMetrica.Puntualidad,
                Velocidad = miVelocidad,
                Satisfaccion = miMetrica.Satisfaccion,
                Productividad = miMetrica.Productividad,
                Retrabajos = miMetrica.Retrabajos,
                Calidad = miMetrica.Calidad,
                Especialidades = miMetrica.Especialidades,
                Capacitacion = miMetrica.Capacitacion
            },
            PromedioEquipo = equipoPromedio,
            Detalle = new
            {
                miMetrica.TotalOrdenes,
                miMetrica.Entregadas,
                miMetrica.Canceladas,
                miMetrica.RetrabajosCantidad,
                miMetrica.EspecialidadesDistintas,
                TotalEspecialidadesActivas = totalEspecialidadesActivas,
                miMetrica.CursosCompletados,
                miMetrica.CursosTotales,
                miMetrica.DuracionPromedioHoras,
                DuracionPromedioGeneralHoras = duracionPromedioGeneral.HasValue ? Math.Round(duracionPromedioGeneral.Value, 1) : (double?)null
            }
        };
    }

    /// <summary>
    /// Ficha de "Portal de Entrega": pensada para presentarse en sala al cliente en el momento de
    /// recoger su vehículo — su historial de visitas, qué se le hizo, quién lo atendió y su experiencia.
    /// </summary>
    public async Task<object?> GetPortalEntregaAsync(Guid ordenId, CancellationToken ct = default)
    {
        var orden = await _db.OrdenesServicio
            .Include(o => o.Cliente)
            .Include(o => o.Vehiculo)
            .Include(o => o.Sucursal)
            .Include(o => o.Conceptos)
            .Include(o => o.Fotos)
            .Include(o => o.MarcasDano)
            .FirstOrDefaultAsync(o => o.Id == ordenId, ct);
        if (orden == null || orden.Cliente == null) return null;

        // ── Visita anterior real (no la actual): la orden más reciente de este cliente antes de esta ──
        var visitaAnterior = await _db.OrdenesServicio
            .Where(o => o.ClienteId == orden.ClienteId && o.Id != ordenId)
            .Include(o => o.Vehiculo)
            .OrderByDescending(o => o.FechaRecepcion)
            .Select(o => new { o.Folio, o.FechaRecepcion, Vehiculo = o.Vehiculo!.Marca + " " + o.Vehiculo!.Modelo })
            .FirstOrDefaultAsync(ct);

        var esPrimeraVisita = orden.Cliente.NumeroVisitas <= 1;

        // ── Equipo de mecánicos (igual criterio que en el resto del sistema: equipo si existe, si no el asignado único) ──
        var asignacionesEquipo = await _db.OrdenesServicioMecanicos.Where(m => m.OrdenServicioId == ordenId).ToListAsync(ct);
        var empleadoIds = asignacionesEquipo.Count > 0
            ? asignacionesEquipo.Select(m => m.EmpleadoId).Distinct().ToList()
            : (orden.EmpleadoAsignadoId.HasValue ? [orden.EmpleadoAsignadoId.Value] : new List<Guid>());

        var mecanicos = await _db.Empleados
            .Include(e => e.Puesto)
            .Where(e => empleadoIds.Contains(e.Id))
            .Select(e => new { Nombre = e.Nombres + " " + e.ApellidoPaterno, Puesto = e.Puesto != null ? e.Puesto.Nombre : null })
            .ToListAsync(ct);

        // ── Conceptos agrupados con nombre de especialidad resuelto ──────────
        var especialidadIds = orden.Conceptos.Where(c => !c.IsDeleted && c.EspecialidadId.HasValue).Select(c => c.EspecialidadId!.Value).Distinct().ToList();
        var especialidadesNombres = especialidadIds.Count > 0
            ? await _db.Catalogos.Where(c => especialidadIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nombre, ct)
            : new Dictionary<Guid, string>();

        var conceptos = orden.Conceptos
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Tipo)
            .Select(c => new
            {
                c.Tipo,
                c.Descripcion,
                c.Cantidad,
                c.Importe,
                Especialidad = c.EspecialidadId.HasValue && especialidadesNombres.TryGetValue(c.EspecialidadId.Value, out var nombreEsp) ? nombreEsp : null
            })
            .ToList();

        // ── Encuesta ya registrada (para no mostrar el formulario dos veces) ──
        var encuestaExistente = await _db.EncuestasSatisfaccion
            .Where(e => e.OrdenServicioId == ordenId)
            .Select(e => new { e.Calificacion, e.Comentario })
            .FirstOrDefaultAsync(ct);

        double? duracionDias = orden.FechaEntrega.HasValue
            ? Math.Round((orden.FechaEntrega.Value - orden.FechaRecepcion).TotalDays, 1)
            : (double?)null;

        return new
        {
            Orden = new
            {
                orden.Id,
                orden.Folio,
                orden.Estado,
                orden.ProblemaReportado,
                orden.FechaRecepcion,
                orden.FechaEntregaProgramada,
                orden.FechaEntrega,
                DuracionDias = duracionDias,
                EntregadoATiempo = orden.EntregadoATiempo,
                orden.Subtotal,
                orden.Descuento,
                orden.Iva,
                orden.Total,
                Sucursal = orden.Sucursal?.Nombre
            },
            Cliente = new
            {
                orden.Cliente.Id,
                orden.Cliente.Nombre,
                orden.Cliente.NumeroVisitas,
                orden.Cliente.PrimeraVisita,
                EsPrimeraVisita = esPrimeraVisita
            },
            VisitaAnterior = visitaAnterior == null ? null : new
            {
                visitaAnterior.Folio,
                visitaAnterior.FechaRecepcion,
                visitaAnterior.Vehiculo
            },
            Vehiculo = orden.Vehiculo == null ? null : new
            {
                orden.Vehiculo.Marca,
                orden.Vehiculo.Modelo,
                orden.Vehiculo.Anio,
                orden.Vehiculo.Placas,
                orden.Vehiculo.Color,
                orden.Vehiculo.Kilometraje
            },
            Mecanicos = mecanicos,
            Conceptos = conceptos,
            Fotos = orden.Fotos.Where(f => !f.IsDeleted).Select(f => new { f.Url, f.Nota }).ToList(),
            DanosRegistrados = orden.MarcasDano.Count,
            Encuesta = encuestaExistente == null ? null : new { encuestaExistente.Calificacion, encuestaExistente.Comentario }
        };
    }

    /// <summary>
    /// Construye los eventos de agenda del taller: por cada orden que tenga alguna fecha relevante
    /// (recepción, entrega programada o entrega real) dentro del rango solicitado, genera un evento
    /// independiente por cada una de esas fechas, para poder pintarlos en un calendario (FullCalendar)
    /// con distintos colores según el tipo y si va a tiempo o con retraso.
    /// </summary>
    public async Task<List<object>> GetAgendaAsync(DateTime desde, DateTime hasta, Guid? sucursalId = null, EstadoOrdenServicio? estado = null, CancellationToken ct = default)
    {
        var query = _db.OrdenesServicio
            .Include(o => o.Cliente)
            .Include(o => o.Vehiculo)
            .Include(o => o.Sucursal)
            .Where(o =>
                (o.FechaRecepcion >= desde && o.FechaRecepcion <= hasta) ||
                (o.FechaEntregaProgramada != null && o.FechaEntregaProgramada >= desde && o.FechaEntregaProgramada <= hasta) ||
                (o.FechaEntrega != null && o.FechaEntrega >= desde && o.FechaEntrega <= hasta))
            .AsQueryable();

        if (sucursalId.HasValue) query = query.Where(o => o.SucursalId == sucursalId.Value);
        if (estado.HasValue) query = query.Where(o => o.Estado == estado.Value);

        var ordenes = await query.ToListAsync(ct);
        var ahora = DateTime.UtcNow;
        var eventos = new List<object>();

        foreach (var o in ordenes)
        {
            var vehiculo = o.Vehiculo != null ? $"{o.Vehiculo.Marca} {o.Vehiculo.Modelo} ({o.Vehiculo.Placas})" : "Sin vehículo";
            var cliente = o.Cliente?.Nombre ?? "Sin cliente";

            if (o.FechaRecepcion >= desde && o.FechaRecepcion <= hasta)
            {
                eventos.Add(new
                {
                    Id = $"{o.Id}-recepcion",
                    OrdenId = o.Id,
                    Tipo = "Recepcion",
                    Title = $"Ingreso — {o.Folio}",
                    Start = o.FechaRecepcion,
                    Color = "#64748B", // slate
                    o.Folio,
                    Cliente = cliente,
                    Vehiculo = vehiculo,
                    Sucursal = o.Sucursal?.Nombre,
                    o.Estado
                });
            }

            if (o.FechaEntregaProgramada.HasValue && o.FechaEntregaProgramada.Value >= desde && o.FechaEntregaProgramada.Value <= hasta)
            {
                var yaEntregado = o.Estado == EstadoOrdenServicio.Entregado;
                var enRiesgo = !yaEntregado && o.FechaEntregaProgramada.Value < ahora;
                eventos.Add(new
                {
                    Id = $"{o.Id}-programada",
                    OrdenId = o.Id,
                    Tipo = "EntregaProgramada",
                    Title = $"Entrega prog. — {o.Folio}",
                    Start = o.FechaEntregaProgramada.Value,
                    Color = yaEntregado ? "#10B981" /* verde, ya se entregó */ : enRiesgo ? "#EF4444" /* rojo, vencida */ : "#3B82F6" /* azul, pendiente */,
                    o.Folio,
                    Cliente = cliente,
                    Vehiculo = vehiculo,
                    Sucursal = o.Sucursal?.Nombre,
                    o.Estado
                });
            }

            if (o.FechaEntrega.HasValue && o.FechaEntrega.Value >= desde && o.FechaEntrega.Value <= hasta)
            {
                eventos.Add(new
                {
                    Id = $"{o.Id}-entrega",
                    OrdenId = o.Id,
                    Tipo = "Entrega",
                    Title = $"Entregado — {o.Folio}",
                    Start = o.FechaEntrega.Value,
                    Color = "#10B981", // verde
                    o.Folio,
                    Cliente = cliente,
                    Vehiculo = vehiculo,
                    Sucursal = o.Sucursal?.Nombre,
                    o.Estado
                });
            }
        }

        return eventos.OrderBy(e => ((dynamic)e).Start).ToList();
    }

    /// <summary>
    /// Métricas reales (histórico completo del tenant) para la presentación ejecutiva:
    /// dinero gestionado en la plataforma, cumplimiento de entregas y satisfacción real
    /// del cliente reportada en las encuestas ya contestadas.
    /// </summary>
    public async Task<object> GetResumenPresentacionAsync(CancellationToken ct = default)
    {
        var ordenesValidas = _db.OrdenesServicio.Where(o => o.Estado != EstadoOrdenServicio.Cancelado);
        var totalOrdenes = await ordenesValidas.CountAsync(ct);
        var totalGestionado = await ordenesValidas.SumAsync(o => o.Total, ct);

        var entregadas = await _db.OrdenesServicio
            .Where(o => o.Estado == EstadoOrdenServicio.Entregado && o.FechaEntrega != null)
            .Select(o => new { o.FechaRecepcion, o.FechaEntrega, o.FechaEntregaProgramada })
            .ToListAsync(ct);

        var conProgramada = entregadas.Where(o => o.FechaEntregaProgramada != null).ToList();
        var aTiempo = conProgramada.Count(o => o.FechaEntrega!.Value <= o.FechaEntregaProgramada!.Value);
        var porcentajeATiempo = conProgramada.Count > 0 ? Math.Round((decimal)aTiempo / conProgramada.Count * 100, 1) : (decimal?)null;

        var tiempoPromedioHoras = entregadas.Count > 0
            ? Math.Round(entregadas.Average(o => (o.FechaEntrega!.Value - o.FechaRecepcion).TotalHours), 1)
            : (double?)null;

        var calificaciones = await _db.EncuestasSatisfaccion.Select(e => e.Calificacion).ToListAsync(ct);
        var promedioSatisfaccion = calificaciones.Count > 0 ? Math.Round(calificaciones.Average(), 2) : (double?)null;
        var distribucionSatisfaccion = Enumerable.Range(1, 5)
            .Select(estrellas => new { Estrellas = estrellas, Cantidad = calificaciones.Count(c => c == estrellas) })
            .ToList();

        var clientesRecurrentes = await _db.Clientes.CountAsync(c => c.NumeroVisitas > 1, ct);
        var clientesTotal = await _db.Clientes.CountAsync(ct);

        // Tendencia de dinero gestionado en la plataforma en los últimos 6 meses.
        var inicioMesActual = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var inicioTendencia = inicioMesActual.AddMonths(-5);
        var ordenesTendencia = await _db.OrdenesServicio
            .Where(o => o.FechaRecepcion >= inicioTendencia && o.Estado != EstadoOrdenServicio.Cancelado)
            .Select(o => new { o.FechaRecepcion, o.Total })
            .ToListAsync(ct);
        var tendencia = Enumerable.Range(0, 6)
            .Select(i => inicioMesActual.AddMonths(-5 + i))
            .Select(mes => new
            {
                Mes = mes.ToString("MMM yyyy", new System.Globalization.CultureInfo("es-MX")),
                Ingresos = ordenesTendencia.Where(o => o.FechaRecepcion.Year == mes.Year && o.FechaRecepcion.Month == mes.Month).Sum(o => o.Total)
            })
            .ToList();

        return new
        {
            TotalOrdenes = totalOrdenes,
            TotalGestionado = totalGestionado,
            PorcentajeATiempo = porcentajeATiempo,
            TiempoPromedioReparacionHoras = tiempoPromedioHoras,
            PromedioSatisfaccion = promedioSatisfaccion,
            TotalEncuestas = calificaciones.Count,
            DistribucionSatisfaccion = distribucionSatisfaccion,
            ClientesRecurrentes = clientesRecurrentes,
            ClientesTotal = clientesTotal,
            Tendencia = tendencia
        };
    }
}
