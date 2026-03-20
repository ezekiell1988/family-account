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

        // ── Seed ──────────────────────────────────────────────────────────────────
        // IdAccountCounterpart referencia el plan contable (AccountConfiguration seed)
        //   44  → 4.1.01.01  ITQS Salario (Ingreso)
        //   15  → 4.3        Otros Ingresos (Ingreso, agrupador)
        //   96  → 5.12       Otros (Gasto, agrupador catch-all)
        //   75  → 5.12.01    Gastos en Pareja (Gasto)
        //   28  → 2.1.01     BAC Credomatic - Tarjetas (Pasivo, agrupador)
        //   42  → 2.2.01.01  Coopealianza Préstamo (Pasivo)
        //   34  → 1.1.03.01  BN Cuenta CRC (Activo) — contrapartida default en transferencias enviadas
        builder.HasData(
            // ── Abonos (entradas) ────────────────────────────────────────────────
            new BankMovementType { IdBankMovementType = 1, CodeBankMovementType = "SAL",        NameBankMovementType = "Depósito de Salario",     IdAccountCounterpart = 44, MovementSign = "Abono", IsActive = true },
            new BankMovementType { IdBankMovementType = 2, CodeBankMovementType = "DEP",        NameBankMovementType = "Depósito en Efectivo",    IdAccountCounterpart = 15, MovementSign = "Abono", IsActive = true },
            new BankMovementType { IdBankMovementType = 3, CodeBankMovementType = "TRANSF-REC", NameBankMovementType = "Transferencia Recibida",  IdAccountCounterpart = 15, MovementSign = "Abono", IsActive = true },

            // ── Cargos (salidas) ─────────────────────────────────────────────────
            new BankMovementType { IdBankMovementType = 4, CodeBankMovementType = "GASTO",      NameBankMovementType = "Gasto General",           IdAccountCounterpart = 96, MovementSign = "Cargo", IsActive = true },
            new BankMovementType { IdBankMovementType = 5, CodeBankMovementType = "RET",        NameBankMovementType = "Retiro en Efectivo",      IdAccountCounterpart = 75, MovementSign = "Cargo", IsActive = true },
            new BankMovementType { IdBankMovementType = 6, CodeBankMovementType = "PAGO-TC",    NameBankMovementType = "Pago Tarjeta de Crédito", IdAccountCounterpart = 28, MovementSign = "Cargo", IsActive = true },
            new BankMovementType { IdBankMovementType = 7, CodeBankMovementType = "PAGO-PREST", NameBankMovementType = "Pago de Préstamo",        IdAccountCounterpart = 42, MovementSign = "Cargo", IsActive = true },
            new BankMovementType { IdBankMovementType = 8, CodeBankMovementType = "TRANSF-ENV", NameBankMovementType = "Transferencia Enviada",   IdAccountCounterpart = 34, MovementSign = "Cargo", IsActive = true }
        );
    }
}
