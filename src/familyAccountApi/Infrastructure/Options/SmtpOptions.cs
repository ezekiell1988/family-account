using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Infrastructure.Options;

public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    [Required(ErrorMessage = "SMTP Host is required")]
    public required string Host { get; init; }

    [Range(1, 65535, ErrorMessage = "SMTP Port must be between 1 and 65535")]
    public int Port { get; init; } = 587;

    [Required(ErrorMessage = "SMTP Username is required")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "SMTP Password is required")]
    public required string Password { get; init; }

    [Required(ErrorMessage = "SMTP FromEmail is required")]
    [EmailAddress(ErrorMessage = "SMTP FromEmail must be a valid email address")]
    public required string FromEmail { get; init; }

    public string FromName { get; init; } = "Family Account";

    public bool EnableSsl { get; init; } = true;
}
