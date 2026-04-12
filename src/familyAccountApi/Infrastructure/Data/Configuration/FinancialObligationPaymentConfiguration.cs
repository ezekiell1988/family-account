using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class FinancialObligationPaymentConfiguration : IEntityTypeConfiguration<FinancialObligationPayment>
{
    public void Configure(EntityTypeBuilder<FinancialObligationPayment> builder)
    {
        builder.ToTable(t => t.HasComment("Pago real registrado contra una cuota del préstamo. Contiene el movimiento BAC vinculado y el asiento contable generado en Borrador."));

        builder.HasKey(x => x.IdFinancialObligationPayment);
        builder.Property(x => x.IdFinancialObligationPayment)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del pago");

        builder.Property(x => x.IdFinancialObligationInstallment)
            .IsRequired()
            .HasComment("FK a la cuota pagada. Índice único: una cuota solo puede tener un pago");

        builder.Property(x => x.IdBankMovement)
            .HasComment("FK al movimiento bancario BAC que originó el pago. Null si no se encuentra el match automático");

        builder.Property(x => x.DatePayment)
            .IsRequired()
            .HasComment("Fecha en que se realizó el pago al banco");

        builder.Property(x => x.AmountPaid)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto total efectivamente pagado");

        builder.Property(x => x.AmountCapitalPaid)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Porción de capital pagada — tomada del Excel");

        builder.Property(x => x.AmountInterestPaid)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Interés pagado — tomado del Excel");

        builder.Property(x => x.AmountLatePaid)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Mora pagada — tomada del Excel. Default 0");

        builder.Property(x => x.AmountOtherPaid)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Otros cargos pagados — tomados del Excel. Default 0");

        builder.Property(x => x.IdAccountingEntry)
            .HasComment("FK al asiento contable generado en Borrador. Null hasta que se genera");

        builder.Property(x => x.IsAutoProcessed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("True = pago detectado y generado automáticamente por sync-excel");

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .HasComment("Observaciones del pago");

        // ── Índice único: una sola cuota → un solo pago ──────────────────────
        builder.HasIndex(x => x.IdFinancialObligationInstallment)
            .IsUnique()
            .HasDatabaseName("UQ_financialObligationPayment_idInstallment");

        // ── FKs ──────────────────────────────────────────────────────────────
        builder.HasOne(x => x.IdFinancialObligationInstallmentNavigation)
            .WithOne(i => i.FinancialObligationPayment)
            .HasForeignKey<FinancialObligationPayment>(x => x.IdFinancialObligationInstallment)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.IdBankMovementNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdBankMovement)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IdAccountingEntryNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdAccountingEntry)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
