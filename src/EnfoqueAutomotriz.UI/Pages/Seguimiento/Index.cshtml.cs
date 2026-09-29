using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Seguimiento;

public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public SeguimientoDto? Data { get; set; }
    public string? Mensaje { get; set; }

    public class SeguimientoDto
    {
        public string Folio { get; set; } = string.Empty;
        public EstadoOrdenServicio Estado { get; set; }
        public EstadoCotizacion EstadoCotizacion { get; set; }
        public string? ProblemaReportado { get; set; }
        public decimal Total { get; set; }
        public string? Cliente { get; set; }
        public string? Vehiculo { get; set; }
        public string? Sucursal { get; set; }
        public List<ConceptoDto> Conceptos { get; set; } = [];
        public List<FotoDto> Fotos { get; set; } = [];
        public List<TimelineDto> Timeline { get; set; } = [];
    }

    public class ConceptoDto { public string Descripcion { get; set; } = ""; public decimal Cantidad { get; set; } public decimal Importe { get; set; } }
    public class FotoDto { public string Url { get; set; } = ""; public string? Nota { get; set; } }
    public class TimelineDto { public EstadoOrdenServicio Estado { get; set; } public string? Comentario { get; set; } public DateTime Fecha { get; set; } }

    public async Task OnGetAsync(Guid token)
    {
        var (data, _) = await _api.GetAsync<SeguimientoDto>($"api/v1/seguimiento/{token}");
        Data = data;
    }

    public async Task<IActionResult> OnPostAprobarAsync(Guid token)
    {
        var (ok, error) = await _api.PatchAsync($"api/v1/seguimiento/{token}/aprobar", new { });
        var (data, _) = await _api.GetAsync<SeguimientoDto>($"api/v1/seguimiento/{token}");
        Data = data;
        Mensaje = ok ? "¡Gracias! Ya estamos trabajando en tu vehículo." : error;
        return Page();
    }

    public async Task<IActionResult> OnPostRechazarAsync(Guid token, string? comentario)
    {
        var (ok, error) = await _api.PatchAsync($"api/v1/seguimiento/{token}/rechazar", new { comentario });
        var (data, _) = await _api.GetAsync<SeguimientoDto>($"api/v1/seguimiento/{token}");
        Data = data;
        Mensaje = ok ? "Hemos registrado tu decisión. Un asesor se pondrá en contacto contigo." : error;
        return Page();
    }

    public async Task<IActionResult> OnPostEncuestaAsync(Guid token, int calificacion, string? comentario)
    {
        await _api.PostAsync<object>($"api/v1/seguimiento/{token}/encuesta", new { calificacion, comentario });
        var (data, _) = await _api.GetAsync<SeguimientoDto>($"api/v1/seguimiento/{token}");
        Data = data;
        Mensaje = "¡Gracias por tu opinión!";
        return Page();
    }
}
