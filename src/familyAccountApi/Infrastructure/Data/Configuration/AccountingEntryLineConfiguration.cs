using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class AccountingEntryLineConfiguration : IEntityTypeConfiguration<AccountingEntryLine>
{
    public void Configure(EntityTypeBuilder<AccountingEntryLine> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Líneas del asiento contable. Cada línea afecta una cuenta contable con un monto al débito o al crédito.");
            t.HasCheckConstraint(
                "CK_accountingEntryLine_singleSidedAmount",
                "((debitAmount > 0 AND creditAmount = 0) OR (debitAmount = 0 AND creditAmount > 0))");
        });

        builder.HasKey(ael => ael.IdAccountingEntryLine);
        builder.Property(ael => ael.IdAccountingEntryLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea del asiento contable.");

        builder.Property(ael => ael.IdAccountingEntry)
            .IsRequired()
            .HasComment("FK al asiento contable al que pertenece la línea.");

        builder.Property(ael => ael.IdAccount)
            .IsRequired()
            .HasComment("FK a la cuenta contable afectada por esta línea.");

        builder.Property(ael => ael.DebitAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto registrado al débito. Debe ser mayor que cero solo cuando la línea es de débito.");

        builder.Property(ael => ael.CreditAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto registrado al crédito. Debe ser mayor que cero solo cuando la línea es de crédito.");

        builder.Property(ael => ael.DescriptionLine)
            .HasMaxLength(300)
            .HasComment("Descripción opcional y específica de la línea del asiento contable.");

        builder.HasIndex(ael => ael.IdAccountingEntry)
            .HasDatabaseName("IX_accountingEntryLine_idAccountingEntry");

        builder.HasIndex(ael => ael.IdAccount)
            .HasDatabaseName("IX_accountingEntryLine_idAccount");

        builder.HasOne(ael => ael.IdAccountingEntryNavigation)
            .WithMany(ae => ae.AccountingEntryLines)
            .HasForeignKey(ael => ael.IdAccountingEntry)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ael => ael.IdAccountNavigation)
            .WithMany(a => a.AccountingEntryLines)
            .HasForeignKey(ael => ael.IdAccount)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
