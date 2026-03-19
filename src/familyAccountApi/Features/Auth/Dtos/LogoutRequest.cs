using System.ComponentModel;

namespace FamilyAccountApi.Features.Auth.Dtos;

public sealed record LogoutRequest
{
    [Description("Refresh token a revocar (opcional)")]
    public string? RefreshToken { get; init; }
}
