using System.Text.Json;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Presentacion;

/// <summary>
/// Presentación ejecutiva de la plataforma: pensada para proyectarse en una junta,
/// resumiendo ahorro de tiempo/costos, escalabilidad y el roadmap (bot de WhatsApp
/// y automatización de procesos). Página autocontenida (sin sidebar) para poder
/// mostrarse a pantalla completa como si fuera un deck.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    private readonly IEmpresaContextService _empresaCtx;
    public IndexModel(IApiService api, IEmpresaContextService empresaCtx)
    {
        _api = api;
        _empresaCtx = empresaCtx;
    }

    public EmpresaContext? Empresa { get; set; }
    public ResumenDto? Resumen { get; set; }
    public string TendenciaJson { get; set; } = "[]";
    public string DistribucionSatisfaccionJson { get; set; } = "[]";

    public class ResumenDto
    {
        public int TotalOrdenes { get; set; }
        public decimal TotalGestionado { get; set; }
        public decimal? PorcentajeATiempo { get; set; }
        public double? TiempoPromedioReparacionHoras { get; set; }
        public double? PromedioSatisfaccion { get; set; }
        public int TotalEncuestas { get; set; }
        public List<DistribucionDto> DistribucionSatisfaccion { get; set; } = [];
        public int ClientesRecurrentes { get; set; }
        public int ClientesTotal { get; set; }
        public List<TendenciaDto> Tendencia { get; set; } = [];
    }

    public class DistribucionDto
    {
        public int Estrellas { get; set; }
        public int Cantidad { get; set; }
    }

    public class TendenciaDto
    {
        public string Mes { get; set; } = "";
        public decimal Ingresos { get; set; }
    }

    public async Task OnGetAsync()
    {
        Empresa = await _empresaCtx.GetAsync();

        var (resumen, _) = await _api.GetAsync<ResumenDto>("api/v1/ordenes-servicio/dashboard/presentacion");
        Resumen = resumen;

        if (resumen != null)
        {
            TendenciaJson = JsonSerializer.Serialize(resumen.Tendencia.Select(t => new { mes = t.Mes, ingresos = t.Ingresos }));
            DistribucionSatisfaccionJson = JsonSerializer.Serialize(resumen.DistribucionSatisfaccion.Select(d => d.Cantidad));
        }
    }
}

