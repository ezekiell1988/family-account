using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable(t =>
            t.HasComment("Monedas disponibles en el sistema contable para registrar operaciones y tipos de cambio."));

        builder.HasKey(c => c.IdCurrency);
        builder.Property(c => c.IdCurrency)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la moneda.");

        builder.Property(c => c.CodeCurrency)
            .HasMaxLength(10)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único de la moneda según estándar internacional. Ejemplo: CRC, USD, EUR.");

        builder.Property(c => c.NameCurrency)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Nombre descriptivo de la moneda. Ejemplo: Colón costarricense, Dólar estadounidense.");

        builder.Property(c => c.SymbolCurrency)
            .HasMaxLength(10)
            .IsRequired()
            .HasComment("Símbolo representativo de la moneda. Ejemplo: ₡, $, €.");

        builder.HasIndex(c => c.CodeCurrency)
            .IsUnique()
            .HasDatabaseName("UQ_currency_codeCurrency");

        builder.HasData(
            new Currency { IdCurrency = 1, CodeCurrency = "CRC", NameCurrency = "Colón costarricense", SymbolCurrency = "₡" },
            new Currency { IdCurrency = 2, CodeCurrency = "USD", NameCurrency = "Dólar estadounidense", SymbolCurrency = "$" });
    }
}