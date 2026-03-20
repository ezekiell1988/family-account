using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankStatementTemplateConfiguration : IEntityTypeConfiguration<BankStatementTemplate>
{
    public void Configure(EntityTypeBuilder<BankStatementTemplate> builder)
    {
        builder.ToTable(t => t.HasComment("Plantillas de carga para extractos bancarios por entidad financiera"));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(b => b.IdBankStatementTemplate);
        builder.Property(b => b.IdBankStatementTemplate)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único de la plantilla");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(b => b.CodeTemplate)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único de la plantilla (p. ej. BCR-CHECKING-2024)");

        builder.Property(b => b.NameTemplate)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre descriptivo de la plantilla");

        builder.Property(b => b.BankName)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre del banco emisor del extracto");

        builder.Property(b => b.ColumnMappings)
            .IsRequired()
            .HasComment("Mapeo de columnas en formato JSON con índices y nombres de campos del Excel");

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si la plantilla está activa para uso");

        // ── Campos opcionales ───────────────────────────────
        builder.Property(b => b.DateFormat)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasComment("Formato de fecha usado en el Excel (p. ej. dd/MM/yyyy)");

        builder.Property(b => b.TimeFormat)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasComment("Formato de hora usado en el Excel (p. ej. HH:mm)");

        builder.Property(b => b.Notes)
            .HasMaxLength(1000)
            .HasComment("Notas o instrucciones adicionales para el uso de la plantilla");

        // ── Índice único ─────────────────────────────────────
        builder.HasIndex(b => b.CodeTemplate)
            .IsUnique()
            .HasDatabaseName("UQ_bankStatementTemplate_codeTemplate");
    }
}
