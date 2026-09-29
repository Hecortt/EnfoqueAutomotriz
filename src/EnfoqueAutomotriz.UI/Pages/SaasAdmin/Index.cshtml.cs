using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.SaasAdmin;

[Authorize(Roles = "0")]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<TenantDto> Tenants { get; set; } = [];
    public string? Mensaje { get; set; }

    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = "";
        public string NombreComercial { get; set; } = "";
        public string Plan { get; set; } = "";
        public string Estado { get; set; } = "";
        public int MaxUsuarios { get; set; }
        public string? ContactoEmail { get; set; }
        public string? ContactoNombre { get; set; }
        public string? RazonSocial { get; set; }
        public string? Rfc { get; set; }
        public long FeaturesActivos { get; set; }
    }

    public async Task OnGetAsync()
    {
        var (tenants, _) = await _api.GetAsync<List<TenantDto>>("api/v1/saas-admin/tenants");
        Tenants = tenants ?? [];
    }

    public async Task<IActionResult> OnPostAsync(string slug, string nuevoPlan)
    {
        var (tenant, _) = await _api.GetAsync<TenantDto>($"api/v1/saas-admin/tenants/{slug}");
        if (tenant != null)
        {
            await _api.PutAsync($"api/v1/saas-admin/tenants/{slug}", new
            {
                nombreComercial = tenant.NombreComercial,
                razonSocial = tenant.RazonSocial,
                rfc = tenant.Rfc,
                plan = nuevoPlan,
                maxUsuarios = tenant.MaxUsuarios,
                contactoEmail = tenant.ContactoEmail,
                contactoNombre = tenant.ContactoNombre,
                fechaVencimiento = (DateTime?)null
            });
            Mensaje = $"Plan de '{slug}' actualizado a {nuevoPlan}. Sus módulos disponibles se ajustaron automáticamente.";
        }

        var (tenants, _) = await _api.GetAsync<List<TenantDto>>("api/v1/saas-admin/tenants");
        Tenants = tenants ?? [];
        return Page();
    }
}
