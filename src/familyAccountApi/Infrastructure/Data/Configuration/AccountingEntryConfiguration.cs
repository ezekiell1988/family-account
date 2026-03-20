using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class AccountingEntryConfiguration : IEntityTypeConfiguration<AccountingEntry>
{
    public void Configure(EntityTypeBuilder<AccountingEntry> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Cabecera del asiento contable. Agrupa las líneas de débito y crédito registradas dentro de un período fiscal determinado.");
            t.HasCheckConstraint("CK_accountingEntry_statusEntry", "statusEntry IN ('Borrador', 'Publicado', 'Anulado')");
        });

        builder.HasKey(ae => ae.IdAccountingEntry);
        builder.Property(ae => ae.IdAccountingEntry)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del asiento contable.");

        builder.Property(ae => ae.IdFiscalPeriod)
            .IsRequired()
            .HasComment("FK al período fiscal al que pertenece el asiento contable.");

        builder.Property(ae => ae.IdCurrency)
            .IsRequired()
            .HasComment("FK a la moneda en la que fue registrado el asiento contable.");

        builder.Property(ae => ae.NumberEntry)
            .HasMaxLength(30)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Número o consecutivo del asiento contable dentro del período fiscal.");

        builder.Property(ae => ae.DateEntry)
            .IsRequired()
            .HasComment("Fecha contable del asiento.");

        builder.Property(ae => ae.DescriptionEntry)
            .HasMaxLength(300)
            .IsRequired()
            .HasComment("Descripción general del asiento contable.");

        builder.Property(ae => ae.StatusEntry)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Estado del asiento contable: Borrador | Publicado | Anulado.");

        builder.Property(ae => ae.ReferenceEntry)
            .HasMaxLength(100)
            .IsUnicode(false)
            .HasComment("Referencia opcional del asiento, como número de documento, factura o comprobante externo.");

        builder.Property(ae => ae.ExchangeRateValue)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Tipo de cambio utilizado al momento de registrar el asiento contable.");

        builder.Property(ae => ae.CreatedAt)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("GETDATE()")
            .HasComment("Fecha y hora de creación del asiento contable.");

        builder.HasIndex(ae => new { ae.IdFiscalPeriod, ae.NumberEntry })
            .IsUnique()
            .HasDatabaseName("UQ_accountingEntry_idFiscalPeriod_numberEntry");

        builder.HasIndex(ae => ae.IdCurrency)
            .HasDatabaseName("IX_accountingEntry_idCurrency");

        builder.HasOne(ae => ae.IdFiscalPeriodNavigation)
            .WithMany(fp => fp.AccountingEntries)
            .HasForeignKey(ae => ae.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ae => ae.IdCurrencyNavigation)
            .WithMany(c => c.AccountingEntries)
            .HasForeignKey(ae => ae.IdCurrency)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
