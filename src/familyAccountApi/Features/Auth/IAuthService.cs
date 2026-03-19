using FamilyAccountApi.Features.Auth.Dtos;

namespace FamilyAccountApi.Features.Auth;

public interface IAuthService
{
    Task<(bool success, string message)> RequestPinAsync(string emailUser, CancellationToken ct = default);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> LogoutAsync(int idUser, string jti, DateTime tokenExpiresAt, string refreshToken, CancellationToken ct = default);
    Task<MeResponse?> GetMeAsync(int idUser, CancellationToken ct = default);
}
