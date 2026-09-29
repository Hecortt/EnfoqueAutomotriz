using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Dashboard;

[Authorize]
public class DesempenoModel : PageModel
{
    private readonly IApiService _api;
    public DesempenoModel(IApiService api) => _api = api;

    public List<TecnicoDto> Tecnicos { get; set; } = [];
    public Guid? EmpleadoId { get; set; }
    public DesempenoDto? Data { get; set; }

    public class TecnicoDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Puesto { get; set; }
        public Guid? SucursalId { get; set; }
    }

    public class EmpleadoInfoDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Puesto { get; set; }
        public string? Sucursal { get; set; }
    }

    public class EjesDto
    {
        public decimal? Puntualidad { get; set; }
        public decimal? Velocidad { get; set; }
        public decimal? Satisfaccion { get; set; }
        public decimal? Productividad { get; set; }
        public decimal? Retrabajos { get; set; }
        public decimal? Calidad { get; set; }
        public decimal? Especialidades { get; set; }
        public decimal? Capacitacion { get; set; }
    }

    public class DetalleDto
    {
        public int TotalOrdenes { get; set; }
        public int Entregadas { get; set; }
        public int Canceladas { get; set; }
        public int RetrabajosCantidad { get; set; }
        public int EspecialidadesDistintas { get; set; }
        public int TotalEspecialidadesActivas { get; set; }
        public int CursosCompletados { get; set; }
        public int CursosTotales { get; set; }
        public double? DuracionPromedioHoras { get; set; }
        public double? DuracionPromedioGeneralHoras { get; set; }
    }

    public class DesempenoDto
    {
        public EmpleadoInfoDto Empleado { get; set; } = new();
        public EjesDto Ejes { get; set; } = new();
        public EjesDto PromedioEquipo { get; set; } = new();
        public DetalleDto Detalle { get; set; } = new();
    }

    public async Task OnGetAsync(Guid? empleadoId)
    {
        var (tecnicos, _) = await _api.GetAsync<List<TecnicoDto>>("api/v1/ordenes-servicio/mecanicos-disponibles");
        Tecnicos = (tecnicos ?? []).OrderBy(t => t.Nombre).ToList();

        EmpleadoId = empleadoId ?? Tecnicos.FirstOrDefault()?.Id;
        if (EmpleadoId.HasValue)
        {
            var (data, _) = await _api.GetAsync<DesempenoDto>($"api/v1/ordenes-servicio/dashboard/desempeno/{EmpleadoId}");
            Data = data;
        }
    }
}
