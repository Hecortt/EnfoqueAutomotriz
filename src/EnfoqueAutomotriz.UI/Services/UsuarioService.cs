using EnfoqueAutomotriz.Domain.Auth.DTOs;

namespace EnfoqueAutomotriz.UI.Services;

public interface IUsuarioService
{
    Task<(List<UsuarioDto>? Value, string? Error)> GetAllAsync(CancellationToken ct = default);
    Task<(UsuarioDto? Value, string? Error)> GetMeAsync(CancellationToken ct = default);
}

public class UsuarioService : IUsuarioService
{
    private readonly IApiService _api;

    public UsuarioService(IApiService api) => _api = api;

    public Task<(List<UsuarioDto>? Value, string? Error)> GetAllAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<UsuarioDto>>("api/v1/auth/usuarios", ct);

    public Task<(UsuarioDto? Value, string? Error)> GetMeAsync(CancellationToken ct = default) =>
        _api.GetAsync<UsuarioDto>("api/v1/auth/me", ct);
}
