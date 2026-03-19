using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Infrastructure.Options;

public sealed class HangfireOptions
{
    public const string Section = "Hangfire";

    [Required]
    public string DashboardPath { get; init; } = "/hangfire";

    [Range(1, 100)]
    public int WorkerCount { get; init; } = 5;

    [Required, MinLength(1)]
    public string[] Queues { get; init; } = ["critical", "default", "integration", "low"];

    [Range(0, 10)]
    public int AutomaticRetryAttempts { get; init; } = 3;
}
