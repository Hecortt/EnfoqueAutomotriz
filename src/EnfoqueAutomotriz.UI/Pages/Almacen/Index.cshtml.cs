using EnfoqueAutomotriz.Domain.Almacen;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Almacen;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<Refaccion> Refacciones { get; set; } = [];

    public async Task OnGetAsync()
    {
        var (data, _) = await _api.GetAsync<List<Refaccion>>("api/v1/almacen/refacciones");
        Refacciones = data ?? [];
    }

    public async Task<IActionResult> OnPostAsync(string nombre, string sku, string? categoria,
        decimal costo, decimal precioVenta, int stockActual, int stockMinimo, int puntoReorden)
    {
        await _api.PostAsync<Refaccion>("api/v1/almacen/refacciones", new
        {
            nombre, sku, categoria, unidad = "pza", costo, precioVenta,
            stockActual, stockMinimo, puntoReorden, activo = true
        });
        return RedirectToPage("/Almacen/Index");
    }
}
