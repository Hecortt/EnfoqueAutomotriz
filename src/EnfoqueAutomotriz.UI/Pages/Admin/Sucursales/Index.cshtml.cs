using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Admin.Sucursales;

[Authorize(Policy = "AccesoTotal")]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<Sucursal> Sucursales { get; set; } = [];

    public async Task OnGetAsync()
    {
        var (data, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        Sucursales = data ?? [];
    }

    public async Task<IActionResult> OnPostAsync(string nombre, string? direccion, string? telefono)
    {
        await _api.PostAsync<Sucursal>("api/v1/sucursales", new { nombre, direccion, telefono, activa = true });
        return RedirectToPage("/Admin/Sucursales/Index");
    }
}
