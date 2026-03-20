using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankMovementConfiguration : IEntityTypeConfiguration<BankMovement>
{
    public void Configure(EntityTypeBuilder<BankMovement> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Encabezado de movimientos bancarios (depósitos, retiros, pagos, etc.)");
            t.HasCheckConstraint("CK_bankMovement_statusMovement", "statusMovement IN ('Borrador', 'Confirmado', 'Anulado')");
        });

        builder.HasKey(bm => bm.IdBankMovement);
        builder.Property(bm => bm.IdBankMovement)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del movimiento bancario");

        builder.Property(bm => bm.IdBankAccount)
            .IsRequired()
            .HasComment("FK a la cuenta bancaria afectada por el movimiento");

        builder.Property(bm => bm.IdBankMovementType)
            .IsRequired()
            .HasComment("FK al tipo de movimiento bancario");

        builder.Property(bm => bm.IdFiscalPeriod)
            .IsRequired()
            .HasComment("FK al período fiscal al que pertenece el movimiento");

        builder.Property(bm => bm.NumberMovement)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Número único del movimiento bancario (ej. MOV-2025-001)");

        builder.Property(bm => bm.DateMovement)
            .IsRequired()
            .HasComment("Fecha del movimiento bancario");

        builder.Property(bm => bm.DescriptionMovement)
            .HasMaxLength(500)
            .IsRequired()
            .HasComment("Descripción del movimiento bancario");

        builder.Property(bm => bm.Amount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto del movimiento en la moneda de la cuenta bancaria");

        builder.Property(bm => bm.StatusMovement)
            .HasMaxLength(15)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Borrador")
            .HasComment("Estado del movimiento: 'Borrador', 'Confirmado' o 'Anulado'");

        builder.Property(bm => bm.ReferenceMovement)
            .HasMaxLength(200)
            .HasComment("Referencia externa del movimiento (número de cheque, comprobante, etc.)");

        builder.Property(bm => bm.ExchangeRateValue)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Tipo de cambio vigente al momento del movimiento");

        builder.Property(bm => bm.CreatedAt)
            .IsRequired()
            .HasComment("Fecha y hora de creación del registro en UTC");

        builder.HasIndex(bm => bm.NumberMovement)
            .IsUnique()
            .HasDatabaseName("UQ_bankMovement_numberMovement");

        builder.HasIndex(bm => bm.IdBankAccount)
            .HasDatabaseName("IX_bankMovement_idBankAccount");

        builder.HasIndex(bm => bm.IdBankMovementType)
            .HasDatabaseName("IX_bankMovement_idBankMovementType");

        builder.HasIndex(bm => bm.IdFiscalPeriod)
            .HasDatabaseName("IX_bankMovement_idFiscalPeriod");

        builder.HasOne(bm => bm.IdBankAccountNavigation)
            .WithMany()
            .HasForeignKey(bm => bm.IdBankAccount)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bm => bm.IdBankMovementTypeNavigation)
            .WithMany(bmt => bmt.BankMovements)
            .HasForeignKey(bm => bm.IdBankMovementType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bm => bm.IdFiscalPeriodNavigation)
            .WithMany()
            .HasForeignKey(bm => bm.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
