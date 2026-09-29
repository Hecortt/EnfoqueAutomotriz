using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Dashboard;

[Authorize]
public class PorSucursalModel : PageModel
{
    private readonly IApiService _api;
    public PorSucursalModel(IApiService api) => _api = api;

    public List<Sucursal> Sucursales { get; set; } = [];
    public Guid? SucursalId { get; set; }
    public SucursalDashboardDto? Data { get; set; }

    public class EstadoDto { public string Estado { get; set; } = ""; public int Cantidad { get; set; } }
    public class TendenciaDto { public string Mes { get; set; } = ""; public decimal Ingresos { get; set; } public decimal Margen { get; set; } }
    public class TecnicoDto { public string Nombre { get; set; } = ""; public int Ordenes { get; set; } }
    public class RefaccionUsoDto { public string Refaccion { get; set; } = ""; public int VecesUsada { get; set; } }

    public class SucursalDashboardDto
    {
        public string Sucursal { get; set; } = "";
        public Guid SucursalId { get; set; }
        public int OrdenesActivas { get; set; }
        public decimal IngresosMes { get; set; }
        public decimal MargenMes { get; set; }
        public decimal MargenPorcentaje { get; set; }
        public int ClientesAtendidos { get; set; }
        public int EmpleadosSucursal { get; set; }
        public List<EstadoDto> PorEstado { get; set; } = [];
        public List<TendenciaDto> Tendencia { get; set; } = [];
        public List<TecnicoDto> TopTecnicos { get; set; } = [];
        public List<RefaccionUsoDto> RefaccionesMasUsadas { get; set; } = [];
        public decimal? EntregasATiempoPorcentaje { get; set; }
        public double? TiempoPromedioReparacionHoras { get; set; }
    }

    public async Task OnGetAsync(Guid? sucursalId)
    {
        var (sucursales, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        Sucursales = sucursales ?? [];

        SucursalId = sucursalId ?? Sucursales.FirstOrDefault()?.Id;
        if (SucursalId.HasValue)
        {
            var (data, _) = await _api.GetAsync<SucursalDashboardDto>($"api/v1/ordenes-servicio/dashboard/sucursal/{SucursalId}");
            Data = data;
        }
    }
}
