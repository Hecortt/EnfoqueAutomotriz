using EnfoqueAutomotriz.Domain.Auth.DTOs;

namespace EnfoqueAutomotriz.UI.Services;

public interface IAuthService
{
    Task<(LoginResponseDto? Response, string? Error)> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
}
