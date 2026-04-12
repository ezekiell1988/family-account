using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class FinancialObligationConfiguration : IEntityTypeConfiguration<FinancialObligation>
{
    public void Configure(EntityTypeBuilder<FinancialObligation> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Cabecera de préstamos y créditos bancarios. Contiene parámetros contables para generación automática de asientos al sincronizar el Excel del banco.");
            t.HasCheckConstraint("CK_financialObligation_statusObligation", "statusObligation IN ('Activo', 'Liquidado')");
        });

        builder.HasKey(x => x.IdFinancialObligation);
        builder.Property(x => x.IdFinancialObligation)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del préstamo u obligación financiera");

        builder.Property(x => x.NameObligation)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre descriptivo. Ej: Préstamo COOPEALIANZA CRC");

        builder.Property(x => x.IdCurrency)
            .IsRequired()
            .HasComment("FK a la moneda del préstamo");

        builder.Property(x => x.OriginalAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto original desembolsado");

        builder.Property(x => x.InterestRate)
            .HasPrecision(8, 4)
            .IsRequired()
            .HasComment("Tasa de interés anual. Ej: 18.5000 = 18.5%");

        builder.Property(x => x.StartDate)
            .IsRequired()
            .HasComment("Fecha de primer desembolso o primer vencimiento");

        builder.Property(x => x.TermMonths)
            .IsRequired()
            .HasComment("Plazo total del préstamo en meses");

        builder.Property(x => x.IdBankAccountPayment)
            .HasComment("FK a la cuenta bancaria BAC desde la que se pagan las cuotas. Null si aún no está configurada");

        builder.Property(x => x.IdAccountLongTerm)
            .IsRequired()
            .HasComment("FK a la cuenta de Pasivo No Corriente del préstamo. Ej: 2.2.01.01");

        builder.Property(x => x.IdAccountShortTerm)
            .IsRequired()
            .HasComment("FK a la cuenta de Pasivo Corriente — porción corriente del préstamo. Ej: 2.1.02.01");

        builder.Property(x => x.IdAccountInterest)
            .IsRequired()
            .HasComment("FK a la cuenta de Gasto Intereses. Ej: 5.5.05");

        builder.Property(x => x.IdAccountLateFee)
            .HasComment("FK a la cuenta de Gasto Mora. Null usa la misma de intereses");

        builder.Property(x => x.IdAccountOther)
            .HasComment("FK a la cuenta de Gasto Otros cargos. Null usa la misma de intereses");

        builder.Property(x => x.MatchKeyword)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Keyword para buscar el movimiento BAC correspondiente. Ej: COOPEALIANZA");

        builder.Property(x => x.StatusObligation)
            .HasMaxLength(10)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Activo")
            .HasComment("Estado del préstamo: Activo | Liquidado");

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .HasComment("Observaciones adicionales del préstamo");

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => x.IdCurrency)
            .HasDatabaseName("IX_financialObligation_idCurrency");

        // ── FKs ──────────────────────────────────────────────────────────────
        builder.HasOne(x => x.IdCurrencyNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdCurrency)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IdBankAccountPaymentNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdBankAccountPayment)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IdAccountLongTermNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdAccountLongTerm)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IdAccountShortTermNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdAccountShortTerm)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IdAccountInterestNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdAccountInterest)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IdAccountLateFeeNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdAccountLateFee)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.IdAccountOtherNavigation)
            .WithMany()
            .HasForeignKey(x => x.IdAccountOther)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
