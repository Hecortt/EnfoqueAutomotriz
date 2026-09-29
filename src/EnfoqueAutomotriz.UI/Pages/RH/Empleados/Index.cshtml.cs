using EnfoqueAutomotriz.Domain.RH;
using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.RH.Empleados;

[Authorize(Policy = "AccesoTotal")]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<Empleado> Empleados { get; set; } = [];
    public List<Puesto> Puestos { get; set; } = [];
    public List<Sucursal> Sucursales { get; set; } = [];

    public async Task OnGetAsync() => await CargarAsync();

    private async Task CargarAsync()
    {
        var (empleados, _) = await _api.GetAsync<List<Empleado>>("api/v1/rh/empleados");
        var (puestos, _) = await _api.GetAsync<List<Puesto>>("api/v1/rh/puestos");
        var (sucursales, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        Empleados = empleados ?? [];
        Puestos = puestos ?? [];
        Sucursales = sucursales ?? [];
    }

    public async Task<IActionResult> OnPostAsync(string nombres, string apellidoPaterno, string? apellidoMaterno,
        Guid puestoId, Guid? sucursalId, Guid? jefeDirectoId, decimal salarioDiario)
    {
        await _api.PostAsync<Empleado>("api/v1/rh/empleados", new
        {
            nombres,
            apellidoPaterno,
            apellidoMaterno,
            puestoId,
            sucursalId,
            jefeDirectoId,
            salarioDiario,
            fechaIngreso = DateTime.UtcNow,
            activo = true
        });
        return RedirectToPage("/RH/Empleados/Index");
    }
}
