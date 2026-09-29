using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Dashboard;

[Authorize]
public class CalidadModel : PageModel
{
    private readonly IApiService _api;
    public CalidadModel(IApiService api) => _api = api;

    public List<Sucursal> Sucursales { get; set; } = [];
    public Guid? SucursalId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string SoloMostrar { get; set; } = "todas"; // todas | atiempo | retrasadas

    public CalidadDashboardDto? Data { get; set; }

    public class ResumenDto
    {
        public int Total { get; set; }
        public int ATiempo { get; set; }
        public int Retrasadas { get; set; }
        public int SinFechaProgramada { get; set; }
        public decimal? PorcentajeATiempo { get; set; }
        public double? PromedioRetrasoHoras { get; set; }
        public double? TiempoPromedioReparacionHoras { get; set; }
    }

    public class EntregaDto
    {
        public Guid Id { get; set; }
        public string Folio { get; set; } = "";
        public string Sucursal { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Vehiculo { get; set; } = "";
        public DateTime FechaRecepcion { get; set; }
        public DateTime? FechaEntregaProgramada { get; set; }
        public DateTime FechaEntrega { get; set; }
        public double DuracionHoras { get; set; }
        public double? DiferenciaHoras { get; set; }
        public bool? EntregadoATiempo { get; set; }
    }

    public class CalidadDashboardDto
    {
        public ResumenDto Resumen { get; set; } = new();
        public List<EntregaDto> Entregas { get; set; } = [];
        public List<TendenciaSemanalDto> TendenciaSemanal { get; set; } = [];
        public List<PorSucursalCalidadDto> PorSucursal { get; set; } = [];
        public List<HistogramaDto> HistogramaRetraso { get; set; } = [];
        public List<RankingMecanicoDto> RankingMecanicos { get; set; } = [];
    }

    public class TendenciaSemanalDto
    {
        public string Semana { get; set; } = "";
        public int Total { get; set; }
        public int ATiempo { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class PorSucursalCalidadDto
    {
        public string Sucursal { get; set; } = "";
        public int Total { get; set; }
        public int ATiempo { get; set; }
        public int Retrasadas { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class HistogramaDto
    {
        public string Rango { get; set; } = "";
        public int Cantidad { get; set; }
    }

    public class RankingMecanicoDto
    {
        public Guid EmpleadoId { get; set; }
        public string Nombre { get; set; } = "";
        public int TotalOrdenes { get; set; }
        public int Retrasos { get; set; }
        public decimal PorcentajeRetraso { get; set; }
        public double? PromedioRetrasoHoras { get; set; }
    }

    public async Task OnGetAsync(Guid? sucursalId, DateTime? desde, DateTime? hasta, string? filtro)
    {
        var (sucursales, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        Sucursales = sucursales ?? [];

        SucursalId = sucursalId;
        Desde = desde;
        Hasta = hasta;
        SoloMostrar = string.IsNullOrWhiteSpace(filtro) ? "todas" : filtro;

        var query = new List<string>();
        if (sucursalId.HasValue) query.Add($"sucursalId={sucursalId}");
        if (desde.HasValue) query.Add($"desde={desde:yyyy-MM-dd}");
        if (hasta.HasValue) query.Add($"hasta={hasta:yyyy-MM-dd}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        var (data, _) = await _api.GetAsync<CalidadDashboardDto>($"api/v1/ordenes-servicio/dashboard/calidad{qs}");
        Data = data;

        if (Data != null && SoloMostrar != "todas")
        {
            Data.Entregas = SoloMostrar switch
            {
                "atiempo" => Data.Entregas.Where(e => e.EntregadoATiempo == true).ToList(),
                "retrasadas" => Data.Entregas.Where(e => e.EntregadoATiempo == false).ToList(),
                _ => Data.Entregas
            };
        }
    }
}
