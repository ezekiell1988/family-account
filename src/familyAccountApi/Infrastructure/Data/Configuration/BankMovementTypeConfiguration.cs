using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankMovementTypeConfiguration : IEntityTypeConfiguration<BankMovementType>
{
    public void Configure(EntityTypeBuilder<BankMovementType> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Catálogo de tipos de movimiento bancario (depósito, retiro, pago, etc.)");
            t.HasCheckConstraint("CK_bankMovementType_movementSign", "movementSign IN ('Cargo', 'Abono')");
        });

        builder.HasKey(bmt => bmt.IdBankMovementType);
        builder.Property(bmt => bmt.IdBankMovementType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del tipo de movimiento bancario");

        builder.Property(bmt => bmt.CodeBankMovementType)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único del tipo de movimiento (ej. DEP, RET, PAGO)");

        builder.Property(bmt => bmt.NameBankMovementType)
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("Nombre descriptivo del tipo de movimiento");

        builder.Property(bmt => bmt.IdAccountCounterpart)
            .IsRequired()
            .HasComment("FK a la cuenta contable contrapartida por defecto para este tipo de movimiento");

        builder.Property(bmt => bmt.MovementSign)
            .HasMaxLength(10)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Signo del movimiento: 'Cargo' (débito a la cuenta bancaria) o 'Abono' (crédito a la cuenta bancaria)");

        builder.Property(bmt => bmt.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si el tipo de movimiento está activo");

        builder.HasIndex(bmt => bmt.CodeBankMovementType)
            .IsUnique()
            .HasDatabaseName("UQ_bankMovementType_codeBankMovementType");

        builder.HasIndex(bmt => bmt.IdAccountCounterpart)
            .HasDatabaseName("IX_bankMovementType_idAccountCounterpart");

        builder.HasOne(bmt => bmt.IdAccountCounterpartNavigation)
            .WithMany()
            .HasForeignKey(bmt => bmt.IdAccountCounterpart)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
