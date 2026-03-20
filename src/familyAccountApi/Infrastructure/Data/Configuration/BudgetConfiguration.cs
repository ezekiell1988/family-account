using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Presupuestos contables por cuenta y período fiscal para control y análisis financiero.");
            t.HasCheckConstraint("CK_budget_amountBudget_positive", "amountBudget > 0");
        });

        builder.HasKey(b => b.IdBudget);
        builder.Property(b => b.IdBudget)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del presupuesto.");

        builder.Property(b => b.IdAccount)
            .IsRequired()
            .HasComment("FK a la cuenta contable asociada al presupuesto.");

        builder.Property(b => b.IdFiscalPeriod)
            .IsRequired()
            .HasComment("FK al período fiscal al que aplica el presupuesto.");

        builder.Property(b => b.AmountBudget)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto presupuestado para la cuenta contable en el período fiscal indicado.");

        builder.Property(b => b.NotesBudget)
            .HasMaxLength(300)
            .HasComment("Notas u observaciones opcionales del presupuesto.");

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si el presupuesto está activo para control y consulta.");

        builder.HasIndex(b => new { b.IdAccount, b.IdFiscalPeriod })
            .IsUnique()
            .HasDatabaseName("UQ_budget_idAccount_idFiscalPeriod");

        builder.HasIndex(b => b.IdFiscalPeriod)
            .HasDatabaseName("IX_budget_idFiscalPeriod");

        builder.HasOne(b => b.IdAccountNavigation)
            .WithMany(a => a.Budgets)
            .HasForeignKey(b => b.IdAccount)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.IdFiscalPeriodNavigation)
            .WithMany(fp => fp.Budgets)
            .HasForeignKey(b => b.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);
    }
}