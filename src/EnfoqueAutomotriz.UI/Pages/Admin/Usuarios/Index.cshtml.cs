using EnfoqueAutomotriz.Domain.Auth.DTOs;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.Admin.Usuarios;

[Authorize(Policy = "AccesoTotal")]
public class IndexModel : PageModel
{
    private readonly IUsuarioService _usuarioService;

    public IndexModel(IUsuarioService usuarioService) => _usuarioService = usuarioService;

    public List<UsuarioDto>? Usuarios { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var (usuarios, error) = await _usuarioService.GetAllAsync();
        Usuarios = usuarios;
        ErrorMessage = error;
    }
}
