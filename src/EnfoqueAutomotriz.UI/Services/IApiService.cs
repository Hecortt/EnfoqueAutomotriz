namespace EnfoqueAutomotriz.UI.Services;

public interface IApiService
{
    Task<(T? Value, string? Error)> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<(T? Value, string? Error)> PostAsync<T>(string endpoint, object body, CancellationToken ct = default);
    Task<(bool Success, string? Error)> PutAsync(string endpoint, object body, CancellationToken ct = default);
    Task<(T? Value, string? Error)> PutAsyncWithResult<T>(string endpoint, object body, CancellationToken ct = default);
    Task<(bool Success, string? Error)> PatchAsync(string endpoint, object body, CancellationToken ct = default);
    Task<(bool Success, string? Error)> DeleteAsync(string endpoint, CancellationToken ct = default);
    Task<(T? Value, string? Error)> PostFormAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default);
    Task<byte[]?> GetBytesAsync(string endpoint, CancellationToken ct = default);
    Task<byte[]?> PostBytesAsync(string endpoint, object body, CancellationToken ct = default);
}
