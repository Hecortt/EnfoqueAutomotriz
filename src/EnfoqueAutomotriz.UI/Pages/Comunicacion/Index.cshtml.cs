using EnfoqueAutomotriz.Domain.Comunicacion;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Comunicacion;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<PlantillaMensaje> Plantillas { get; set; } = [];
    public List<MensajeEnviado> Historial { get; set; } = [];

    public async Task OnGetAsync() => await CargarAsync();

    private async Task CargarAsync()
    {
        var (plantillas, _) = await _api.GetAsync<List<PlantillaMensaje>>("api/v1/comunicacion/plantillas");
        var (historial, _) = await _api.GetAsync<List<MensajeEnviado>>("api/v1/comunicacion/historial");
        Plantillas = plantillas ?? [];
        Historial = historial ?? [];
    }

    public async Task<IActionResult> OnPostAsync(string nombre, string cuerpo, int canal)
    {
        await _api.PostAsync<PlantillaMensaje>("api/v1/comunicacion/plantillas", new
        {
            nombre, cuerpo, canal, activo = true
        });
        return RedirectToPage("/Comunicacion/Index");
    }
}
