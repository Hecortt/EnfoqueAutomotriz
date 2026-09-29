using EnfoqueAutomotriz.Domain.RH;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.RH.Organigrama;

[Authorize(Policy = "AccesoTotal")]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<OrganigramaNodo> Nodos { get; set; } = [];

    public async Task OnGetAsync()
    {
        var (data, _) = await _api.GetAsync<List<OrganigramaNodo>>("api/v1/rh/organigrama");
        Nodos = data ?? [];
    }
}
