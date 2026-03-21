using System.Text;

namespace FamilyAccountApi.Infrastructure.Extensions;

public static class ConfigurationExtensions
{
    private static string? TryDecodeBase64(string? value)
    {
        if (value is null) return null;
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        catch { return null; }
    }

    public static string RequireConnectionString(
        this IConfiguration config,
        string kvKey,
        string base64ConfigKey,
        string envVar)
        => config[kvKey]
            ?? TryDecodeBase64(config.GetConnectionString(base64ConfigKey))
            ?? TryDecodeBase64(Environment.GetEnvironmentVariable(envVar))
            ?? throw new InvalidOperationException(
                $"Connection string no encontrado. Configure '{kvKey}' en Key Vault, " +
                $"'ConnectionStrings:{base64ConfigKey}' en appsettings (base64) " +
                $"o la variable de entorno '{envVar}' en base64.");
}
