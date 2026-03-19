using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Infrastructure.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> User => Set<User>();
    public DbSet<UserPin> UserPin => Set<UserPin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Aplicar convención camelCase a tablas y columnas
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.GetTableName() is { } tableName)
                entityType.SetTableName(ToCamelCase(tableName));

            foreach (var property in entityType.GetProperties())
            {
                var colName = property.GetColumnName();
                if (!string.IsNullOrEmpty(colName))
                    property.SetColumnName(ToCamelCase(colName));
            }
        }
    }

    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
