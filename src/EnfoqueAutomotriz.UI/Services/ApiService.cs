using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnfoqueAutomotriz.Utility;

namespace EnfoqueAutomotriz.UI.Services;

public class ApiService : IApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private record APIResponseWrapper<T>(bool IsSuccess, T? Result, List<string>? ErrorMessages);

    public ApiService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("Api");

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogError("[ApiService] HttpContext es null - no se puede obtener token");
            return client;
        }

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return client;
        }

        var token = user.FindFirst(SD.AccessToken)?.Value;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[ApiService] Claim '{ClaimType}' no encontrado", SD.AccessToken);
            return client;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenantSlug = ResolveTenantSlug(httpContext);
        if (!string.IsNullOrEmpty(tenantSlug))
            client.DefaultRequestHeaders.Add("X-Tenant-Slug", tenantSlug);

        return client;
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

    public async Task<(T? Value, string? Error)> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync(endpoint, ct);
            string json = await response.Content.ReadAsStringAsync(ct) ?? "";

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GET {Endpoint} -> {Status}: {Body}", endpoint, (int)response.StatusCode, json);
                return (default, ExtractError(json, response.StatusCode));
            }

            return (ExtractResult<T>(json), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GET {Endpoint}", endpoint);
            return (default, ex.Message);
        }
    }

    public async Task<(T? Value, string? Error)> PostAsync<T>(string endpoint, object body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content, ct);
            string json = await response.Content.ReadAsStringAsync(ct) ?? "";

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("POST {Endpoint} -> {Status}: {Body}", endpoint, (int)response.StatusCode, json);
                return (default, ExtractError(json, response.StatusCode));
            }

            return (ExtractResult<T>(json), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en POST {Endpoint}", endpoint);
            return (default, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> PutAsync(string endpoint, object body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PutAsync(endpoint, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("PUT {Endpoint} -> {Status}: {Body}", endpoint, (int)response.StatusCode, json);
                return (false, ExtractError(json, response.StatusCode));
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PUT {Endpoint}", endpoint);
            return (false, ex.Message);
        }
    }

    public async Task<(T? Value, string? Error)> PutAsyncWithResult<T>(string endpoint, object body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PutAsync(endpoint, content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PUT {Endpoint} -> {Status}: {Body}", endpoint, (int)response.StatusCode, json);
                return (default, ExtractError(json, response.StatusCode));
            }
            return (ExtractResult<T>(json), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PUT {Endpoint}", endpoint);
            return (default, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> PatchAsync(string endpoint, object body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = content };
            var response = await client.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("PATCH {Endpoint} -> {Status}: {Body}", endpoint, (int)response.StatusCode, json);
                return (false, ExtractError(json, response.StatusCode));
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en PATCH {Endpoint}", endpoint);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.DeleteAsync(endpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("DELETE {Endpoint} -> {Status}: {Body}", endpoint, (int)response.StatusCode, json);
                return (false, ExtractError(json, response.StatusCode));
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en DELETE {Endpoint}", endpoint);
            return (false, ex.Message);
        }
    }

    public async Task<(T? Value, string? Error)> PostFormAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.PostAsync(endpoint, content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("POST FORM {Endpoint} -> {Status}: {Body}", endpoint, (int)response.StatusCode, json);
                return (default, ExtractError(json, response.StatusCode));
            }

            return (ExtractResult<T>(json), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en POST FORM {Endpoint}", endpoint);
            return (default, ex.Message);
        }
    }

    public async Task<byte[]?> GetBytesAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync(endpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GET BYTES {Endpoint} -> {Status}", endpoint, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GET BYTES {Endpoint}", endpoint);
            return null;
        }
    }

    public async Task<byte[]?> PostBytesAsync(string endpoint, object body, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("POST BYTES {Endpoint} -> {Status}", endpoint, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en POST BYTES {Endpoint}", endpoint);
            return null;
        }
    }

    private T? ExtractResult<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            var wrapper = JsonSerializer.Deserialize<APIResponseWrapper<T>>(json, _jsonOptions);
            if (wrapper is { IsSuccess: true })
                return wrapper.Result;

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
    }

    private static string ExtractError(string json, System.Net.HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(json))
            return $"Error {(int)statusCode}";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("errorMessages", out var errMsgs) && errMsgs.ValueKind == JsonValueKind.Array)
            {
                var messages = errMsgs.EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
                if (messages.Count > 0)
                    return string.Join(". ", messages);
            }

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                return error.GetString() ?? json;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                if (root.TryGetProperty("innerDetail", out var innerDetail) && innerDetail.ValueKind == JsonValueKind.String)
                {
                    var inner = innerDetail.GetString();
                    if (!string.IsNullOrEmpty(inner))
                        return inner;
                }
                return detail.GetString() ?? json;
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                return title.GetString() ?? json;
        }
        catch
        {
            // Si no se puede parsear, devolver el JSON crudo
        }

        return json.Length > 200 ? json[..200] + "..." : json;
    }
}
