using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FamilyAccountApi.Infrastructure.Data;

/// <summary>
/// Factory usada exclusivamente por las herramientas de EF Core (dotnet ef migrations).
/// En producción el DbContext se registra en Program.cs con el connection string real.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // En design-time: decodifica el base64 desde la variable de entorno
        // o usa el connection string de dev hardcodeado para generar migraciones localmente
        string connectionString;
        var b64 = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING_BASE64");
        if (b64 is not null)
        {
            connectionString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }
        else
        {
            // Fallback de desarrollo — mismo server que appsettings.Development.json
            connectionString = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(
                    "U2VydmVyPTE3Mi4xOTEuMTI4LjI0LDE0MzM7RGF0YWJhc2U9ZGJmYTtVc2VyIElkPXNhO1Bhc3N3b3JkPVNxbGZlNmZjZTQ4IUVGRTc7VHJ1c3RTZXJ2ZXJDZXJ0aWZpY2F0ZT1UcnVlO011bHRpcGxlQWN0aXZlUmVzdWx0U2V0cz10cnVl"));
        }

        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
