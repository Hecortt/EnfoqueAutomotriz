using System.Security.Claims;
using EnfoqueAutomotriz.Domain.Auth.DTOs;
using EnfoqueAutomotriz.UI.Services;
using EnfoqueAutomotriz.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnfoqueAutomotriz.UI.Pages;

public class LoginModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public LoginModel(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public bool RecaptchaEnabled => _configuration.GetValue<bool>("Recaptcha:Enabled");
    public string RecaptchaSiteKey => _configuration.GetValue<string>("Recaptcha:SiteKey") ?? "";

    public class InputModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? RecaptchaToken { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        ReturnUrl = returnUrl ?? Url.Content("~/");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        var loginRequest = new LoginRequestDto
        {
            Email = Input.Email,
            Password = Input.Password,
            RecaptchaToken = RecaptchaEnabled ? Input.RecaptchaToken : null
        };

        var (response, error) = await _authService.LoginAsync(loginRequest);

        if (response == null)
        {
            ErrorMessage = error ?? "Error al iniciar sesión";
            return Page();
        }

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, response.Id.ToString()),
            new(ClaimTypes.Email, response.Email),
            new(ClaimTypes.Name, response.NombreCompleto),
            new(ClaimTypes.Role, response.Rol.ToString()),
            new(SD.AccessToken, response.Token)
        ];

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = response.Expiration
            });

        return LocalRedirect(ReturnUrl);
    }
}
