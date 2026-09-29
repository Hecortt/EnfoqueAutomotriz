using EnfoqueAutomotriz.Domain.Clientes;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Clientes;

[Authorize]
public class DetailModel : PageModel
{
    private readonly IApiService _api;
    public DetailModel(IApiService api) => _api = api;

    public Cliente? Cliente { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public Guid ClienteId { get; set; }
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Anio { get; set; } = DateTime.UtcNow.Year;
        public string Placas { get; set; } = string.Empty;
        public int Kilometraje { get; set; }
    }

    public async Task OnGetAsync(Guid id)
    {
        var (cliente, _) = await _api.GetAsync<Cliente>($"api/v1/clientes/{id}");
        Cliente = cliente;
        Input.ClienteId = id;
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        await _api.PostAsync<Vehiculo>($"api/v1/clientes/{id}/vehiculos", new
        {
            marca = Input.Marca,
            modelo = Input.Modelo,
            anio = Input.Anio,
            placas = Input.Placas,
            kilometraje = Input.Kilometraje
        });

        return RedirectToPage("/Clientes/Detail", new { id });
    }
}
