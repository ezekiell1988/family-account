using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Infrastructure.Options;

public sealed class AppOptions
{
    public const string Section = "App";

    [Required]
    public string NameCustomer { get; init; } = "Family Account";

    public string SloganCustomer { get; init; } = string.Empty;

    [Required]
    public string Version { get; init; } = "1.0.0";
}
