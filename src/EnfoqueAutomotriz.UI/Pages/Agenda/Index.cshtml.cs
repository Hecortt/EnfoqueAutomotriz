using System.Globalization;
using System.Text.Json;
using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Agenda;

/// <summary>
/// Agenda general del taller: calendario mensual con todas las órdenes de servicio —
/// cuándo entraron, cuándo se comprometió su entrega y cuándo se entregaron realmente —
/// para tener de un vistazo la carga de trabajo de cualquier día.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<Sucursal> Sucursales { get; set; } = [];
    public Guid? SucursalId { get; set; }
    public EstadoOrdenServicio? Estado { get; set; }

    public int Anio { get; set; }
    public int Mes { get; set; }
    public string NombreMes { get; set; } = "";
    public string EventosJson { get; set; } = "[]";

    public class EventoAgendaDto
    {
        public string Id { get; set; } = "";
        public Guid OrdenId { get; set; }
        public string Tipo { get; set; } = "";
        public string Title { get; set; } = "";
        public DateTime Start { get; set; }
        public string Color { get; set; } = "";
        public string Folio { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Vehiculo { get; set; } = "";
        public string? Sucursal { get; set; }
        public EstadoOrdenServicio Estado { get; set; }
    }

    public async Task OnGetAsync(int? anio, int? mes, Guid? sucursalId, EstadoOrdenServicio? estado)
    {
        var hoy = DateTime.Now;
        Anio = anio ?? hoy.Year;
        Mes = (mes.HasValue && mes.Value >= 1 && mes.Value <= 12) ? mes.Value : hoy.Month;
        SucursalId = sucursalId;
        Estado = estado;
        NombreMes = new DateTime(Anio, Mes, 1).ToString("MMMM yyyy", new CultureInfo("es-MX"));

        var (sucursales, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        Sucursales = sucursales ?? [];

        // Rango con margen de una semana a cada lado para cubrir los días de los meses
        // adyacentes que se alcanzan a ver en la cuadrícula del calendario.
        var primerDia = new DateTime(Anio, Mes, 1);
        var ultimoDia = primerDia.AddMonths(1).AddDays(-1);
        var desde = primerDia.AddDays(-7);
        var hasta = ultimoDia.AddDays(7);

        var query = new List<string> { $"desde={desde:yyyy-MM-dd}", $"hasta={hasta:yyyy-MM-dd}" };
        if (sucursalId.HasValue) query.Add($"sucursalId={sucursalId}");
        if (estado.HasValue) query.Add($"estado={(int)estado.Value}");
        var qs = "?" + string.Join("&", query);

        var (eventos, _) = await _api.GetAsync<List<EventoAgendaDto>>($"api/v1/ordenes-servicio/agenda{qs}");

        var eventosCalendario = (eventos ?? []).Select(e => new
        {
            id = e.Id,
            title = e.Title,
            start = e.Start.ToString("s"),
            color = e.Color,
            url = $"/OrdenesServicio/Detail/{e.OrdenId}",
            extendedProps = new
            {
                tipo = e.Tipo,
                folio = e.Folio,
                cliente = e.Cliente,
                vehiculo = e.Vehiculo,
                sucursal = e.Sucursal,
                estado = e.Estado.ToString()
            }
        });

        EventosJson = JsonSerializer.Serialize(eventosCalendario);
    }
}
