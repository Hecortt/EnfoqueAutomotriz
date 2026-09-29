using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public DashboardEmpresaDto? Kpis { get; set; }

    public class SucursalKpiDto
    {
        public Guid SucursalId { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public int Ordenes { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Margen { get; set; }
    }

    public class TendenciaDto
    {
        public string Mes { get; set; } = string.Empty;
        public decimal Ingresos { get; set; }
        public decimal Margen { get; set; }
    }

    public class EstadoDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class DashboardEmpresaDto
    {
        public int SucursalesTotal { get; set; }
        public int EmpleadosTotal { get; set; }
        public int OrdenesActivas { get; set; }
        public decimal IngresosMes { get; set; }
        public decimal MargenMes { get; set; }
        public decimal MargenPorcentaje { get; set; }
        public List<SucursalKpiDto> PorSucursal { get; set; } = [];
        public int ClientesRecurrentes { get; set; }
        public int ClientesTotal { get; set; }
        public List<TendenciaDto> Tendencia { get; set; } = [];
        public List<EstadoDto> PorEstado { get; set; } = [];
        public decimal? EntregasATiempoPorcentaje { get; set; }
        public double? TiempoPromedioReparacionHoras { get; set; }
    }

    public async Task OnGetAsync()
    {
        var (kpis, _) = await _api.GetAsync<DashboardEmpresaDto>("api/v1/ordenes-servicio/dashboard/empresa");
        Kpis = kpis;
    }
}
