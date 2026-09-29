using EnfoqueAutomotriz.Domain.Ordenes;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.OrdenesServicio;

/// <summary>
/// Portal de Entrega público (sin login): accesible con el mismo token único de
/// seguimiento de la orden (<see cref="OrdenServicio.TokenSeguimiento"/>), ya sea
/// mostrado en sala por el asesor al momento de la entrega, o enviado como link/QR
/// al cliente para que lo consulte por su cuenta.
/// </summary>
public class EntregaModel : PageModel
{
    private readonly IApiService _api;
    private readonly IEmpresaContextService _empresaCtx;

    public EntregaModel(IApiService api, IEmpresaContextService empresaCtx)
    {
        _api = api;
        _empresaCtx = empresaCtx;
    }

    public PortalEntregaDto? Data { get; set; }
    public EmpresaContext? Empresa { get; set; }
    public string? Mensaje { get; set; }
    public Guid Token { get; set; }

    public class OrdenDto
    {
        public Guid Id { get; set; }
        public string Folio { get; set; } = "";
        public EstadoOrdenServicio Estado { get; set; }
        public string? ProblemaReportado { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public DateTime? FechaEntregaProgramada { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public double? DuracionDias { get; set; }
        public bool? EntregadoATiempo { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public string? Sucursal { get; set; }
    }

    public class ClienteDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = "";
        public int NumeroVisitas { get; set; }
        public DateTime? PrimeraVisita { get; set; }
        public bool EsPrimeraVisita { get; set; }
    }

    public class VisitaAnteriorDto
    {
        public string Folio { get; set; } = "";
        public DateTime FechaRecepcion { get; set; }
        public string? Vehiculo { get; set; }
    }

    public class VehiculoDto
    {
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Anio { get; set; }
        public string Placas { get; set; } = "";
        public string? Color { get; set; }
        public int Kilometraje { get; set; }
    }

    public class MecanicoDto
    {
        public string Nombre { get; set; } = "";
        public string? Puesto { get; set; }
    }

    public class ConceptoDto
    {
        public TipoConceptoOrden Tipo { get; set; }
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal Importe { get; set; }
        public string? Especialidad { get; set; }
    }

    public class FotoDto
    {
        public string Url { get; set; } = "";
        public string? Nota { get; set; }
    }

    public class EncuestaDto
    {
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
    }

    public class PortalEntregaDto
    {
        public OrdenDto Orden { get; set; } = new();
        public ClienteDto Cliente { get; set; } = new();
        public VisitaAnteriorDto? VisitaAnterior { get; set; }
        public VehiculoDto? Vehiculo { get; set; }
        public List<MecanicoDto> Mecanicos { get; set; } = [];
        public List<ConceptoDto> Conceptos { get; set; } = [];
        public List<FotoDto> Fotos { get; set; } = [];
        public int DanosRegistrados { get; set; }
        public EncuestaDto? Encuesta { get; set; }
    }

    public async Task OnGetAsync(Guid token)
    {
        Token = token;
        Empresa = await _empresaCtx.GetAsync();
        var (data, _) = await _api.GetAsync<PortalEntregaDto>($"api/v1/seguimiento/{token}/entrega");
        Data = data;
    }

    public async Task<IActionResult> OnPostEncuestaAsync(Guid token, int calificacion, string? comentario)
    {
        Token = token;
        await _api.PostAsync<object>($"api/v1/seguimiento/{token}/entrega/encuesta", new { calificacion, comentario });
        Empresa = await _empresaCtx.GetAsync();
        var (data, _) = await _api.GetAsync<PortalEntregaDto>($"api/v1/seguimiento/{token}/entrega");
        Data = data;
        Mensaje = "¡Gracias por compartir tu experiencia con nosotros!";
        return Page();
    }
}
