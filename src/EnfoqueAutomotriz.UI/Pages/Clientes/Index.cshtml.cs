using EnfoqueAutomotriz.Domain.Clientes;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Clientes;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<Cliente>? Clientes { get; set; }
    public string? Query { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }

    public async Task OnGetAsync(string? q)
    {
        Query = q;
        var endpoint = string.IsNullOrWhiteSpace(q) ? "api/v1/clientes" : $"api/v1/clientes?q={Uri.EscapeDataString(q)}";
        var (clientes, _) = await _api.GetAsync<List<Cliente>>(endpoint);
        Clientes = clientes ?? [];
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (_, error) = await _api.PostAsync<Cliente>("api/v1/clientes", new
        {
            nombre = Input.Nombre,
            telefono = Input.Telefono,
            email = Input.Email
        });

        if (error != null)
        {
            ErrorMessage = error;
            var (clientes, _) = await _api.GetAsync<List<Cliente>>("api/v1/clientes");
            Clientes = clientes ?? [];
            return Page();
        }

        return RedirectToPage("/Clientes/Index");
    }
}
