using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankStatementTransactionConfiguration : IEntityTypeConfiguration<BankStatementTransaction>
{
    public void Configure(EntityTypeBuilder<BankStatementTransaction> builder)
    {
        builder.ToTable(t => t.HasComment("Transacciones individuales importadas de extractos bancarios"));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(b => b.IdBankStatementTransaction);
        builder.Property(b => b.IdBankStatementTransaction)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único de la transacción del extracto");

        // ── FK ──────────────────────────────────────────────
        builder.Property(b => b.IdBankStatementImport)
            .IsRequired()
            .HasComment("Importación a la que pertenece esta transacción");

        builder.Property(b => b.IdAccountingEntry)
            .HasComment("Asiento contable asociado para conciliación (opcional)");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(b => b.AccountingDate)
            .IsRequired()
            .HasComment("Fecha contable de la transacción según el banco");

        builder.Property(b => b.TransactionDate)
            .IsRequired()
            .HasComment("Fecha real de ejecución de la transacción");

        builder.Property(b => b.Description)
            .HasMaxLength(500)
            .IsRequired()
            .HasComment("Descripción de la transacción proporcionada por el banco");

        builder.Property(b => b.IsReconciled)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indica si la transacción ha sido conciliada con un asiento contable");

        // ── Campos opcionales ───────────────────────────────
        builder.Property(b => b.TransactionTime)
            .HasComment("Hora de la transacción si está disponible");

        builder.Property(b => b.DocumentNumber)
            .HasMaxLength(100)
            .IsUnicode(false)
            .HasComment("Número de documento o referencia de la transacción");

        builder.Property(b => b.DebitAmount)
            .HasPrecision(18, 2)
            .HasComment("Monto de débito (retiro o pago)");

        builder.Property(b => b.CreditAmount)
            .HasPrecision(18, 2)
            .HasComment("Monto de crédito (depósito o ingreso)");

        builder.Property(b => b.Balance)
            .HasPrecision(18, 2)
            .HasComment("Saldo resultante después de la transacción");

        builder.Property(b => b.IdBankMovementType)
            .HasComment("Tipo de movimiento bancario asignado (auto o manual). Null si a\u00fan no fue clasificado");

        builder.Property(b => b.IdAccountCounterpart)
            .HasComment("Cuenta contable contrapartida asignada manualmente (sobrescribe la del tipo de movimiento)");
        builder.Property(b => b.IdCostCenter)
            .HasComment("Centro de costo asignado manualmente a la transacción (opcional)");
        // ── Relaciones ──────────────────────────────────────
        builder.HasOne(b => b.IdBankStatementImportNavigation)
            .WithMany(i => i.BankStatementTransactions)
            .HasForeignKey(b => b.IdBankStatementImport)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.IdBankMovementTypeNavigation)
            .WithMany()
            .HasForeignKey(b => b.IdBankMovementType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.IdAccountCounterpartNavigation)
            .WithMany()
            .HasForeignKey(b => b.IdAccountCounterpart)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.IdCostCenterNavigation)
            .WithMany()
            .HasForeignKey(b => b.IdCostCenter)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.IdAccountingEntryNavigation)
            .WithMany()
            .HasForeignKey(b => b.IdAccountingEntry)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Índices ─────────────────────────────────────────
        builder.HasIndex(b => b.IdBankStatementImport)
            .HasDatabaseName("IX_bankStatementTransaction_idBankStatementImport");

        builder.HasIndex(b => b.AccountingDate)
            .HasDatabaseName("IX_bankStatementTransaction_accountingDate");

        builder.HasIndex(b => b.IsReconciled)
            .HasDatabaseName("IX_bankStatementTransaction_isReconciled");

        builder.HasIndex(b => b.IdAccountingEntry)
            .HasDatabaseName("IX_bankStatementTransaction_idAccountingEntry");

        builder.HasIndex(b => b.IdBankMovementType)
            .HasDatabaseName("IX_bankStatementTransaction_idBankMovementType");
    }
}
