using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Auth.Dtos;

public sealed record RefreshTokenRequest
{
    [Required]
    [Description("Refresh token obtenido en el login")]
    public required string RefreshToken { get; init; }
}
