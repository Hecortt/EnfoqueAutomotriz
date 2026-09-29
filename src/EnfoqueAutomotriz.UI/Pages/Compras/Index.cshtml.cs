using EnfoqueAutomotriz.Domain.Almacen;
using EnfoqueAutomotriz.Domain.Compras;
using EnfoqueAutomotriz.Domain.Sucursales;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Compras;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<OrdenCompra> Ordenes { get; set; } = [];
    public List<Proveedor> Proveedores { get; set; } = [];
    public List<Sucursal> Sucursales { get; set; } = [];
    public List<Refaccion> Refacciones { get; set; } = [];

    public async Task OnGetAsync() => await CargarAsync();

    private async Task CargarAsync()
    {
        var (ordenes, _) = await _api.GetAsync<List<OrdenCompra>>("api/v1/compras/ordenes");
        var (proveedores, _) = await _api.GetAsync<List<Proveedor>>("api/v1/compras/proveedores");
        var (sucursales, _) = await _api.GetAsync<List<Sucursal>>("api/v1/sucursales");
        var (refacciones, _) = await _api.GetAsync<List<Refaccion>>("api/v1/almacen/refacciones");
        Ordenes = ordenes ?? [];
        Proveedores = proveedores ?? [];
        Sucursales = sucursales ?? [];
        Refacciones = refacciones ?? [];
    }

    public async Task<IActionResult> OnPostCrearAsync(Guid proveedorId, Guid sucursalId, Guid refaccionId, int cantidad, decimal costoUnitario)
    {
        await _api.PostAsync<OrdenCompra>("api/v1/compras/ordenes", new
        {
            proveedorId,
            sucursalId,
            fechaEsperada = (DateTime?)null,
            detalles = new[] { new { refaccionId, cantidad, costoUnitario } }
        });
        return RedirectToPage("/Compras/Index");
    }

    public async Task<IActionResult> OnPostCrearProveedorAsync(string nombre, string? telefono)
    {
        await _api.PostAsync<Proveedor>("api/v1/compras/proveedores", new { nombre, telefono });
        return RedirectToPage("/Compras/Index");
    }

    public async Task<IActionResult> OnPostRecibirAsync(Guid ordenId)
    {
        await _api.PatchAsync($"api/v1/compras/ordenes/{ordenId}/recibir", new { });
        return RedirectToPage("/Compras/Index");
    }
}
