using EnfoqueAutomotriz.Domain.RH;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.RH.Capacitacion;

[Authorize(Policy = "AccesoTotal")]
public class IndexModel : PageModel
{
    private readonly IApiService _api;
    public IndexModel(IApiService api) => _api = api;

    public List<Domain.RH.Capacitacion> Cursos { get; set; } = [];
    public List<Empleado> Empleados { get; set; } = [];
    public List<EmpleadoCapacitacion> Asignaciones { get; set; } = [];

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync() => await CargarAsync();

    private async Task CargarAsync()
    {
        var (cursos, _) = await _api.GetAsync<List<Domain.RH.Capacitacion>>("api/v1/rh/capacitaciones");
        var (empleados, _) = await _api.GetAsync<List<Empleado>>("api/v1/rh/empleados");
        var (asignaciones, _) = await _api.GetAsync<List<EmpleadoCapacitacion>>("api/v1/rh/capacitaciones/asignaciones");

        Cursos = cursos ?? [];
        Empleados = (empleados ?? []).Where(e => e.Activo).OrderBy(e => e.Nombres).ToList();
        Asignaciones = (asignaciones ?? []).OrderByDescending(a => a.FechaAsignacion).ToList();
    }

    public async Task<IActionResult> OnPostCrearCursoAsync(string titulo, string? categoria, int duracionHoras, string? descripcion)
    {
        await _api.PostAsync<Domain.RH.Capacitacion>("api/v1/rh/capacitaciones", new
        {
            titulo,
            categoria,
            duracionHoras,
            descripcion
        });
        return RedirectToPage("/RH/Capacitacion/Index");
    }

    public async Task<IActionResult> OnPostAsignarAsync(Guid empleadoId, Guid capacitacionId)
    {
        var (_, error) = await _api.PostAsync<EmpleadoCapacitacion>("api/v1/rh/capacitaciones/asignaciones", new
        {
            empleadoId,
            capacitacionId
        });

        if (error != null)
        {
            ErrorMessage = error;
            await CargarAsync();
            return Page();
        }

        return RedirectToPage("/RH/Capacitacion/Index");
    }

    public async Task<IActionResult> OnPostActualizarEstadoAsync(Guid asignacionId, int estado, decimal? calificacionFinal)
    {
        await _api.PatchAsync($"api/v1/rh/capacitaciones/asignaciones/{asignacionId}", new
        {
            estado,
            calificacionFinal
        });
        return RedirectToPage("/RH/Capacitacion/Index");
    }

    public async Task<IActionResult> OnPostEliminarAsignacionAsync(Guid asignacionId)
    {
        await _api.DeleteAsync($"api/v1/rh/capacitaciones/asignaciones/{asignacionId}");
        return RedirectToPage("/RH/Capacitacion/Index");
    }
}
