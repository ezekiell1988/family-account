using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class FinancialObligationInstallmentConfiguration : IEntityTypeConfiguration<FinancialObligationInstallment>
{
    public void Configure(EntityTypeBuilder<FinancialObligationInstallment> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Tabla de amortización del préstamo. Una fila por cuota. Se sincroniza automáticamente al cargar el Excel del banco.");
            t.HasCheckConstraint("CK_financialObligationInstallment_status", "statusInstallment IN ('Pendiente', 'Vigente', 'Pagada', 'Vencida')");
        });

        builder.HasKey(x => x.IdFinancialObligationInstallment);
        builder.Property(x => x.IdFinancialObligationInstallment)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único de la cuota");

        builder.Property(x => x.IdFinancialObligation)
            .IsRequired()
            .HasComment("FK al préstamo al que pertenece esta cuota");

        builder.Property(x => x.NumberInstallment)
            .IsRequired()
            .HasComment("Número de cuota según el Excel del banco. Clave natural para upsert. Ej: 1, 2 … 36");

        builder.Property(x => x.DueDate)
            .IsRequired()
            .HasComment("Fecha de vencimiento de la cuota según el banco");

        builder.Property(x => x.BalanceAfter)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Saldo del préstamo después de pagar esta cuota");

        builder.Property(x => x.AmountCapital)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Porción de capital que amortiza el principal");

        builder.Property(x => x.AmountInterest)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Gasto financiero del período");

        builder.Property(x => x.AmountLateFee)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Mora por pago tardío. Default 0");

        builder.Property(x => x.AmountOther)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Otros cargos adicionales del banco. Default 0");

        builder.Property(x => x.AmountTotal)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Total de la cuota: Capital + Interés + Mora + Otros");

        builder.Property(x => x.StatusInstallment)
            .HasMaxLength(10)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Pendiente")
            .HasComment("Estado según el Excel: Pendiente | Vigente | Pagada | Vencida");

        builder.Property(x => x.SyncedAt)
            .HasComment("Fecha y hora UTC en que el Excel actualizó por última vez esta fila");

        // ── Índice único: una cuota por número dentro del préstamo ───────────
        builder.HasIndex(x => new { x.IdFinancialObligation, x.NumberInstallment })
            .IsUnique()
            .HasDatabaseName("UQ_financialObligationInstallment_idObligation_number");

        // ── FK ───────────────────────────────────────────────────────────────
        builder.HasOne(x => x.IdFinancialObligationNavigation)
            .WithMany(o => o.FinancialObligationInstallments)
            .HasForeignKey(x => x.IdFinancialObligation)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
