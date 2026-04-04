using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductOptionGroupConfiguration : IEntityTypeConfiguration<ProductOptionGroup>
{
    public void Configure(EntityTypeBuilder<ProductOptionGroup> builder)
    {
        builder.ToTable(t => t.HasComment("Grupos de opciones configurables de un producto (ej: Tamaño, Masa, Sabor). Un producto con HasOptions=true puede tener N grupos."));

        builder.HasKey(g => g.IdProductOptionGroup);
        builder.Property(g => g.IdProductOptionGroup)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del grupo de opciones.");

        builder.Property(g => g.IdProduct)
            .IsRequired()
            .HasComment("FK al producto configurable al que pertenece este grupo.");

        builder.Property(g => g.NameGroup)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre visible del grupo (ej: Elige tu tamaño).");

        builder.Property(g => g.IsRequired)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Si el cliente debe elegir obligatoriamente en este grupo.");

        builder.Property(g => g.MinSelections)
            .IsRequired()
            .HasDefaultValue(1)
            .HasComment("Mínimo de items a elegir. 0 para grupos opcionales.");

        builder.Property(g => g.MaxSelections)
            .IsRequired()
            .HasDefaultValue(1)
            .HasComment("Máximo de items a elegir. 1 para exclusivo, N para múltiple.");

        builder.Property(g => g.AllowSplit)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Cuando true, en modo mitad/mitad el cliente asigna cada selección a una mitad (half1|half2|whole). Aplica a grupos de sabor y adicionales.");

        builder.Property(g => g.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Orden de presentación del grupo al cliente.");

        // ── FK: Product ─────────────────────────────────────────────────────
        builder.HasOne(g => g.IdProductNavigation)
            .WithMany(p => p.ProductOptionGroups)
            .HasForeignKey(g => g.IdProduct)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
