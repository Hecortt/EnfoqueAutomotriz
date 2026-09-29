using EnfoqueAutomotriz.Domain.Promociones;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Promociones;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<Promocion> Promociones { get; set; } = [];

    public async Task OnGetAsync()
    {
        var (data, _) = await _api.GetAsync<List<Promocion>>("api/v1/promociones");
        Promociones = data ?? [];
    }

    public async Task<IActionResult> OnPostAsync(string nombre, string? descripcion, int tipoDescuento,
        decimal valor, DateTime fechaInicio, DateTime fechaFin, int aplicaA)
    {
        await _api.PostAsync<Promocion>("api/v1/promociones", new
        {
            nombre, descripcion, tipoDescuento, valor,
            fechaInicio = fechaInicio.ToUniversalTime(),
            fechaFin = fechaFin.ToUniversalTime(),
            aplicaA, activo = true
        });
        return RedirectToPage("/Promociones/Index");
    }
}
