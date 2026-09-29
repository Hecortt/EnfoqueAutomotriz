using EnfoqueAutomotriz.Domain.Clientes;
using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.OrdenesServicio;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IApiService _api;
    public CreateModel(IApiService api) => _api = api;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Sucursal> Sucursales { get; set; } = [];
    public List<Cliente> Clientes { get; set; } = [];
    public List<Vehiculo> Vehiculos { get; set; } = [];
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        public Guid SucursalId { get; set; }
        public Guid ClienteId { get; set; }
        public Guid VehiculoId { get; set; }
        public int KilometrajeRecepcion { get; set; }
        public string? NivelGasolina { get; set; }
        public string ProblemaReportado { get; set; } = string.Empty;
        public DateTime? FechaEntregaProgramada { get; set; }
    }

    public async Task OnGetAsync() => await CargarCatalogosAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var (_, error) = await _api.PostAsync<OrdenServicio>("api/v1/ordenes-servicio", new
        {
            sucursalId = Input.SucursalId,
            clienteId = Input.ClienteId,
            vehiculoId = Input.VehiculoId,
            empleadoAsignadoId = (Guid?)null,
            problemaReportado = Input.ProblemaReportado,
            kilometrajeRecepcion = Input.KilometrajeRecepcion,
            nivelGasolina = Input.NivelGasolina,
            observaciones = (string?)null,
            fechaEntregaProgramada = Input.FechaEntregaProgramada
        });

        if (error != null)
        {
            ErrorMessage = error;
            await CargarCatalogosAsync();
            return Page();
        }

        return RedirectToPage("/OrdenesServicio/Index");
    }

    private async Task CargarCatalogosAsync()
    {
        var (sucursales, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        var (clientes, _) = await _api.GetAsync<List<Cliente>>("api/v1/clientes");
        var (vehiculos, _) = await _api.GetAsync<List<Vehiculo>>("api/v1/vehiculos");
        Sucursales = sucursales ?? [];
        Clientes = clientes ?? [];
        Vehiculos = vehiculos ?? [];
    }
}
