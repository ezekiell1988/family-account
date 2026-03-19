using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required(ErrorMessage = "JWT Secret is required")]
    [MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters")]
    public required string Secret { get; init; }

    [Required(ErrorMessage = "JWT Issuer is required")]
    public required string Issuer { get; init; }

    [Required(ErrorMessage = "JWT Audience is required")]
    public required string Audience { get; init; }

    [Range(1, 1440, ErrorMessage = "TokenExpirationMinutes must be between 1 and 1440")]
    public int TokenExpirationMinutes { get; init; } = 60;

    [Range(1, 90, ErrorMessage = "RefreshTokenExpirationDays must be between 1 and 90")]
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
