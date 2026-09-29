using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnfoqueAutomotriz.Api.Extensions;

public class RecaptchaSettings
{
    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public float MinimumScore { get; set; } = 0.5f;
    public bool Enabled { get; set; } = false;
}

public interface IRecaptchaService
{
    Task<(bool IsValid, float Score, string? Error)> ValidateTokenAsync(string? token, CancellationToken ct = default);
    bool IsEnabled { get; }
}

public class RecaptchaService : IRecaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly RecaptchaSettings _settings;
    private readonly ILogger<RecaptchaService> _logger;

    public RecaptchaService(HttpClient httpClient, RecaptchaSettings settings, ILogger<RecaptchaService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public bool IsEnabled => _settings.Enabled && !string.IsNullOrEmpty(_settings.SecretKey);

    public async Task<(bool IsValid, float Score, string? Error)> ValidateTokenAsync(string? token, CancellationToken ct = default)
    {
        if (!IsEnabled)
            return (true, 1.0f, null);

        if (string.IsNullOrWhiteSpace(token))
            return (false, 0f, "Token de reCAPTCHA no proporcionado");

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _settings.SecretKey,
                ["response"] = token
            });

            var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<RecaptchaResponse>(json);
            if (result == null)
                return (false, 0f, "Respuesta de reCAPTCHA inválida");

            if (!result.Success)
            {
                var errors = string.Join(", ", result.ErrorCodes ?? []);
                _logger.LogWarning("reCAPTCHA validation failed: {Errors}", errors);
                return (false, 0f, $"Validación de reCAPTCHA fallida: {errors}");
            }

            if (result.Score < _settings.MinimumScore)
                return (false, result.Score, $"Puntuación de seguridad insuficiente ({result.Score:F2})");

            return (true, result.Score, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reCAPTCHA token");
            return (true, 1.0f, null);
        }
    }

    private class RecaptchaResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("score")] public float Score { get; set; }
        [JsonPropertyName("error-codes")] public List<string>? ErrorCodes { get; set; }
    }
}

public static class RecaptchaExtensions
{
    public static IServiceCollection AddRecaptcha(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new RecaptchaSettings();
        configuration.GetSection("Recaptcha").Bind(settings);
        services.AddSingleton(settings);

        services.AddHttpClient<IRecaptchaService, RecaptchaService>();

        return services;
    }
}
