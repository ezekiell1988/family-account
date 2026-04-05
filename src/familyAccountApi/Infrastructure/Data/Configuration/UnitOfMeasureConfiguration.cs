using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo global de unidades de medida utilizadas en productos, recetas e inventario."));

        builder.HasKey(u => u.IdUnit);
        builder.Property(u => u.IdUnit)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la unidad de medida.");

        builder.Property(u => u.CodeUnit)
            .HasMaxLength(10)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código corto de la unidad: ML, GR, KG, LTR, BOT160, LATA400, UNI, etc.");

        builder.Property(u => u.NameUnit)
            .HasMaxLength(80)
            .IsRequired()
            .HasComment("Nombre legible de la unidad: Mililitro, Gramo, Botella 160ml, etc.");

        builder.HasOne(u => u.UnitType)
            .WithMany(ut => ut.UnitsOfMeasure)
            .HasForeignKey(u => u.IdUnitType)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_unitOfMeasure_unitType");

        builder.HasIndex(u => u.CodeUnit)
            .IsUnique()
            .HasDatabaseName("UQ_unitOfMeasure_codeUnit");

        // ── Seed ──────────────────────────────────────────────────────────────
        builder.HasData(
            new UnitOfMeasure { IdUnit = 1, CodeUnit = "U",  NameUnit = "Unidad",       IdUnitType = 1 },
            new UnitOfMeasure { IdUnit = 2, CodeUnit = "M3", NameUnit = "Metro Cúbico", IdUnitType = 2 },
            new UnitOfMeasure { IdUnit = 3, CodeUnit = "KG", NameUnit = "Kilogramo",    IdUnitType = 3 },
            new UnitOfMeasure { IdUnit = 4, CodeUnit = "M",  NameUnit = "Metro",        IdUnitType = 4 }
        );
    }
}
