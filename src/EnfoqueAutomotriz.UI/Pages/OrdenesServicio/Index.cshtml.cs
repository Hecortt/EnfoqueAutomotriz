using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.OrdenesServicio;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<OrdenServicio>? Ordenes { get; set; }

    public async Task OnGetAsync()
    {
        var (ordenes, _) = await _api.GetAsync<List<OrdenServicio>>("api/v1/ordenes-servicio");
        Ordenes = ordenes ?? [];
    }
}
