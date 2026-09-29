using EnfoqueAutomotriz.Domain.Configuracion;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Configuracion;

[Authorize(Policy = "AccesoTotal")]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    private readonly IEmpresaContextService _empresaCtx;

    public IndexModel(IApiService api, IEmpresaContextService empresaCtx)
    {
        _api = api;
        _empresaCtx = empresaCtx;
    }

    [BindProperty]
    public ConfiguracionEmpresa Input { get; set; } = new();

    public string? Mensaje { get; set; }
    public bool Exito { get; set; }

    public async Task OnGetAsync()
    {
        var (data, _) = await _api.GetAsync<ConfiguracionEmpresa>("api/v1/configuracion-empresa");
        Input = data ?? new ConfiguracionEmpresa();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (exito, error) = await _api.PutAsync("api/v1/configuracion-empresa", Input);
        Exito = exito;
        Mensaje = exito ? "Datos de la empresa guardados correctamente." : (error ?? "No se pudo guardar la configuración.");

        if (exito)
        {
            _empresaCtx.Invalidar();
        }

        return Page();
    }
}
