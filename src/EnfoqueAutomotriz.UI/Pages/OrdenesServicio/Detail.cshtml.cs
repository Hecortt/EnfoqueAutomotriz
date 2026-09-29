using EnfoqueAutomotriz.Domain.Catalogs;
using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.OrdenesServicio;

[Authorize]
public class DetailModel : PageModel
{
    private readonly IApiService _api;
    public DetailModel(IApiService api) => _api = api;

    public OrdenServicio? Orden { get; set; }
    public List<Catalogo> Especialidades { get; set; } = [];

    public async Task OnGetAsync(Guid id)
    {
        var (orden, _) = await _api.GetAsync<OrdenServicio>($"api/v1/ordenes-servicio/{id}");
        Orden = orden;

        var (especialidades, _) = await _api.GetAsync<List<Catalogo>>("api/v1/catalogs/especialidades");
        Especialidades = especialidades ?? [];
    }

    public async Task<IActionResult> OnPostAddConceptoAsync(Guid ordenId, string descripcion, int tipo, decimal cantidad, decimal precioUnitario, Guid? especialidadId)
    {
        await _api.PostAsync<OrdenServicioConcepto>($"api/v1/ordenes-servicio/{ordenId}/conceptos", new
        {
            tipo,
            refaccionId = (Guid?)null,
            descripcion,
            cantidad,
            precioUnitario,
            costoUnitario = precioUnitario * 0.6m, // estimado simple de costo cuando no se liga a refacción específica
            especialidadId
        });
        return RedirectToPage("/OrdenesServicio/Detail", new { id = ordenId });
    }

    public async Task<IActionResult> OnPostAddFotoAsync(Guid ordenId, string url, string? nota)
    {
        await _api.PostAsync<OrdenServicioFoto>($"api/v1/ordenes-servicio/{ordenId}/fotos", new
        {
            url,
            nota,
            estadoAlSubir = (int)EstadoOrdenServicio.Diagnostico
        });
        return RedirectToPage("/OrdenesServicio/Detail", new { id = ordenId });
    }

    public async Task<IActionResult> OnPostCambiarEstadoAsync(Guid ordenId, int nuevoEstado)
    {
        await _api.PatchAsync($"api/v1/ordenes-servicio/{ordenId}/estado", new
        {
            estado = nuevoEstado,
            comentario = (string?)null
        });
        return RedirectToPage("/OrdenesServicio/Detail", new { id = ordenId });
    }

    public async Task<IActionResult> OnPostEnviarCotizacionAsync(Guid ordenId)
    {
        await _api.PostAsync<object>($"api/v1/ordenes-servicio/{ordenId}/enviar-cotizacion", new { });
        return RedirectToPage("/OrdenesServicio/Detail", new { id = ordenId });
    }

    public async Task<IActionResult> OnPostSetFechaProgramadaAsync(Guid ordenId, DateTime? fechaEntregaProgramada)
    {
        await _api.PatchAsync($"api/v1/ordenes-servicio/{ordenId}/fecha-programada", new
        {
            fechaEntregaProgramada
        });
        return RedirectToPage("/OrdenesServicio/Detail", new { id = ordenId });
    }

    public async Task<IActionResult> OnPostUpdateProblemaAsync(Guid ordenId, string problemaReportado)
    {
        await _api.PatchAsync($"api/v1/ordenes-servicio/{ordenId}/problema-reportado", new
        {
            problemaReportado
        });
        return RedirectToPage("/OrdenesServicio/Detail", new { id = ordenId });
    }

    public async Task<IActionResult> OnPostAddDanoAsync(Guid ordenId, decimal x, decimal y, int tipo, string? descripcion)
    {
        await _api.PostAsync<OrdenServicioMarcaDano>($"api/v1/ordenes-servicio/{ordenId}/danos", new
        {
            x,
            y,
            tipo,
            descripcion
        });
        return RedirectToPage("/OrdenesServicio/Detail", new { id = ordenId });
    }

    public async Task<IActionResult> OnPostRemoveDanoAsync(Guid ordenId, Guid marcaId)
    {
        await _api.DeleteAsync($"api/v1/ordenes-servicio/danos/{marcaId}");
        return RedirectToPage("/OrdenesServicio/Detail", new { id = ordenId });
    }
}
