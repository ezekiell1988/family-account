using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankStatementImportConfiguration : IEntityTypeConfiguration<BankStatementImport>
{
    public void Configure(EntityTypeBuilder<BankStatementImport> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Registro de importaciones de extractos bancarios");
            t.HasCheckConstraint(
                "CK_bankStatementImport_status",
                "status IN ('Pending', 'Processing', 'Completed', 'Failed')"
            );
        });

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(b => b.IdBankStatementImport);
        builder.Property(b => b.IdBankStatementImport)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único de la importación");

        // ── FK ──────────────────────────────────────────────
        builder.Property(b => b.IdBankAccount)
            .IsRequired()
            .HasComment("Cuenta bancaria asociada a la importación");

        builder.Property(b => b.IdBankStatementTemplate)
            .IsRequired()
            .HasComment("Plantilla utilizada para procesar el extracto");

        builder.Property(b => b.ImportedBy)
            .IsRequired()
            .HasComment("Usuario que realizó la importación");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(b => b.FileName)
            .HasMaxLength(500)
            .IsRequired()
            .HasComment("Nombre del archivo Excel importado");

        builder.Property(b => b.ImportDate)
            .IsRequired()
            .HasComment("Fecha y hora de la importación");

        builder.Property(b => b.Status)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Pending")
            .HasComment("Estado de la importación: Pending, Processing, Completed, Failed");

        builder.Property(b => b.TotalTransactions)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Número total de transacciones en el archivo");

        builder.Property(b => b.ProcessedTransactions)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Número de transacciones procesadas exitosamente");

        // ── Campos opcionales ───────────────────────────────
        builder.Property(b => b.ErrorMessage)
            .HasMaxLength(2000)
            .HasComment("Mensaje de error en caso de fallo en la importación");

        // ── Relaciones ──────────────────────────────────────
        builder.HasOne(b => b.IdBankAccountNavigation)
            .WithMany()
            .HasForeignKey(b => b.IdBankAccount)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.IdBankStatementTemplateNavigation)
            .WithMany(t => t.BankStatementImports)
            .HasForeignKey(b => b.IdBankStatementTemplate)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ImportedByNavigation)
            .WithMany()
            .HasForeignKey(b => b.ImportedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Índices ─────────────────────────────────────────
        builder.HasIndex(b => b.IdBankAccount)
            .HasDatabaseName("IX_bankStatementImport_idBankAccount");

        builder.HasIndex(b => b.IdBankStatementTemplate)
            .HasDatabaseName("IX_bankStatementImport_idBankStatementTemplate");

        builder.HasIndex(b => b.ImportDate)
            .HasDatabaseName("IX_bankStatementImport_importDate");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_bankStatementImport_status");
    }
}
