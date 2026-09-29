using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Dashboard;

[Authorize]
public class OperativoModel : PageModel
{
    private readonly IApiService _api;
    public OperativoModel(IApiService api) => _api = api;

    public List<Sucursal> Sucursales { get; set; } = [];
    public Guid? SucursalId { get; set; }
    public List<OrdenServicio> Ordenes { get; set; } = [];

    public async Task OnGetAsync(Guid? sucursalId)
    {
        var (sucursales, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        Sucursales = sucursales ?? [];
        SucursalId = sucursalId;

        var qs = sucursalId.HasValue ? $"?sucursalId={sucursalId}" : "";
        var (ordenes, _) = await _api.GetAsync<List<OrdenServicio>>($"api/v1/ordenes-servicio{qs}");
        Ordenes = (ordenes ?? [])
            .Where(o => o.Estado != EstadoOrdenServicio.Entregado && o.Estado != EstadoOrdenServicio.Cancelado)
            .ToList();
    }
}
