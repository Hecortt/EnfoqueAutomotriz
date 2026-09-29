using EnfoqueAutomotriz.Domain.Almacen;
using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Almacen;

[Authorize]
public class SolicitudesModel : PageModel
{
    private readonly IApiService _api;
    public SolicitudesModel(IApiService api) => _api = api;

    public List<Sucursal> Sucursales { get; set; } = [];
    public Guid? SucursalId { get; set; }
    public bool SoloEnProceso { get; set; } = true;
    public List<SolicitudAlmacen> Solicitudes { get; set; } = [];

    public async Task OnGetAsync(Guid? sucursalId, bool soloEnProceso = true)
    {
        var (sucursales, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        Sucursales = sucursales ?? [];
        SucursalId = sucursalId;
        SoloEnProceso = soloEnProceso;

        var qs = sucursalId.HasValue ? $"?sucursalId={sucursalId}" : "";
        var (solicitudes, _) = await _api.GetAsync<List<SolicitudAlmacen>>($"api/v1/almacen/solicitudes{qs}");
        Solicitudes = solicitudes ?? [];

        if (SoloEnProceso)
        {
            var estadosEnProceso = new[] { EstadoOrdenServicio.Diagnostico, EstadoOrdenServicio.EsperandoAprobacion, EstadoOrdenServicio.EnReparacion };
            Solicitudes = Solicitudes.Where(s => s.OrdenServicio != null && estadosEnProceso.Contains(s.OrdenServicio.Estado)).ToList();
        }
    }
}
