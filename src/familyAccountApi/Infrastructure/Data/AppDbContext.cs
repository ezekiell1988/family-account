using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Infrastructure.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>     User     => Set<User>();
    public DbSet<UserPin>  UserPin  => Set<UserPin>();
    public DbSet<Role>     Role     => Set<Role>();
    public DbSet<UserRole> UserRole => Set<UserRole>();
    public DbSet<Contact>            Contact            => Set<Contact>();
    public DbSet<ContactType>         ContactType        => Set<ContactType>();
    public DbSet<ContactContactType>  ContactContactType => Set<ContactContactType>();
    public DbSet<ProductSKU>          ProductSKU         => Set<ProductSKU>();
    public DbSet<Product>                  Product                  => Set<Product>();
    public DbSet<ProductProductSKU>        ProductProductSKU        => Set<ProductProductSKU>();
    public DbSet<ProductCategory>          ProductCategory          => Set<ProductCategory>();
    public DbSet<ProductProductCategory>   ProductProductCategory   => Set<ProductProductCategory>();
    public DbSet<Account>                  Account                  => Set<Account>();
    public DbSet<FiscalPeriod>             FiscalPeriod             => Set<FiscalPeriod>();
    public DbSet<AccountingEntry>          AccountingEntry          => Set<AccountingEntry>();
    public DbSet<AccountingEntryLine>      AccountingEntryLine      => Set<AccountingEntryLine>();

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
