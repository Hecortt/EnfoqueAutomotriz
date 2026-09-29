using System.Text;
using System.Text.Json;
using EnfoqueAutomotriz.Domain.Auth.DTOs;

namespace EnfoqueAutomotriz.UI.Services;

public class AuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record APIResponseWrapper<T>(bool IsSuccess, T? Result, List<string>? ErrorMessages);

    public AuthService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<(LoginResponseDto? Response, string? Error)> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");

            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                var slug = ResolveTenantSlug(ctx);
                if (!string.IsNullOrEmpty(slug))
                    client.DefaultRequestHeaders.Add("X-Tenant-Slug", slug);
            }

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/v1/auth/login", content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (null, "Email o contraseña incorrectos");
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    return (null, "Usuario bloqueado o inactivo. Contacta al administrador.");
                return (null, $"Error al iniciar sesión ({(int)response.StatusCode})");
            }

            var wrapper = JsonSerializer.Deserialize<APIResponseWrapper<LoginResponseDto>>(json, _jsonOptions);
            return (wrapper?.Result, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se puede conectar al API en {BaseUrl}",
                _httpClientFactory.CreateClient("Api").BaseAddress);
            return (null, $"No se puede conectar al servidor API. Verifica que esté en ejecución. ({ex.Message})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en Login");
            return (null, $"Error inesperado: {ex.Message}");
        }
    }

    private static string? ResolveTenantSlug(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("X-Tenant-Slug", out var headerSlug)
            && !string.IsNullOrWhiteSpace(headerSlug))
            return headerSlug.ToString().ToLowerInvariant().Trim();

        var host = ctx.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length >= 3)
        {
            var subdomain = parts[0].ToLowerInvariant();
            if (subdomain is not ("www" or "api" or "app" or "admin" or "localhost"))
                return subdomain;
        }

        return null;
    }
}
