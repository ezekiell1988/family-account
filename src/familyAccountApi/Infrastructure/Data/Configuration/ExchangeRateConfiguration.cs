using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Tipos de cambio por moneda y fecha para soportar operaciones multi-moneda en el sistema contable.");
            t.HasCheckConstraint("CK_exchangeRate_rateValue_positive", "rateValue > 0");
        });

        builder.HasKey(er => er.IdExchangeRate);
        builder.Property(er => er.IdExchangeRate)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del tipo de cambio.");

        builder.Property(er => er.IdCurrency)
            .IsRequired()
            .HasComment("FK a la moneda a la que pertenece este tipo de cambio.");

        builder.Property(er => er.RateDate)
            .IsRequired()
            .HasComment("Fecha efectiva del tipo de cambio.");

        builder.Property(er => er.RateValue)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Valor del tipo de cambio de la moneda respecto a la moneda base definida por la organización.");

        builder.HasIndex(er => new { er.IdCurrency, er.RateDate })
            .IsUnique()
            .HasDatabaseName("UQ_exchangeRate_idCurrency_rateDate");

        builder.HasIndex(er => er.IdCurrency)
            .HasDatabaseName("IX_exchangeRate_idCurrency");

        builder.HasOne(er => er.IdCurrencyNavigation)
            .WithMany(c => c.ExchangeRates)
            .HasForeignKey(er => er.IdCurrency)
            .OnDelete(DeleteBehavior.Restrict);
    }
}