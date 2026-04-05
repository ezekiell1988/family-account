using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductOptionItemConfiguration : IEntityTypeConfiguration<ProductOptionItem>
{
    public void Configure(EntityTypeBuilder<ProductOptionItem> builder)
    {
        builder.ToTable(t => t.HasComment("Cada opción dentro de un grupo configurable (ej: Delgada, Gruesa, Rellena dentro del grupo Masa)."));

        builder.HasKey(i => i.IdProductOptionItem);
        builder.Property(i => i.IdProductOptionItem)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del item de opción.");

        builder.Property(i => i.IdProductOptionGroup)
            .IsRequired()
            .HasComment("FK al grupo de opciones al que pertenece este item.");

        builder.Property(i => i.NameItem)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre visible de la opción (ej: Masa Delgada).");

        builder.Property(i => i.PriceDelta)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Ajuste de precio sobre el precio base del producto. Puede ser positivo, negativo o cero.");

        builder.Property(i => i.IsDefault)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Opción marcada por defecto al abrir el selector.");

        builder.Property(i => i.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Orden de presentación dentro del grupo.");

        // ── FK: ProductOptionGroup ───────────────────────────────────────────
        builder.HasOne(i => i.IdProductOptionGroupNavigation)
            .WithMany(g => g.ProductOptionItems)
            .HasForeignKey(i => i.IdProductOptionGroup)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: ProductRecipe (opcional) ─────────────────────────────────────
        builder.Property(i => i.IdProductRecipe)
            .HasComment("FK opcional a la receta que se usa para producir este option item (ej: receta de masa delgada).");

        builder.HasOne(i => i.IdProductRecipeNavigation)
            .WithMany()
            .HasForeignKey(i => i.IdProductRecipe)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
