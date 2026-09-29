using EnfoqueAutomotriz.Domain.Auth.DTOs;
using EnfoqueAutomotriz.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages.MiPerfil;

[Authorize]
public class MiPerfilModel : PageModel
{
    private readonly IUsuarioService _usuarioService;

    public MiPerfilModel(IUsuarioService usuarioService) => _usuarioService = usuarioService;

    public UsuarioDto? Usuario { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var (usuario, error) = await _usuarioService.GetMeAsync();
        Usuario = usuario;
        ErrorMessage = error;
    }
}
