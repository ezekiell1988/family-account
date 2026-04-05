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

        // ── Seed: opciones para Pizza (grupos 1-4) ────────────────────────────
        // Grupo 1 — Tamaño: Mediana (default, sin receta) | Grande (+$2, receta 6)
        // Grupo 2 — Masa: Clásica (default) | Delgada
        // Grupo 3 — Sabor: Pepperoni (default, receta 4) | Hawaiian (receta 5)
        // Grupo 4 — Extras: Doble Queso (+$0.75, receta 7)
        builder.HasData(
            new ProductOptionItem { IdProductOptionItem = 1, IdProductOptionGroup = 1, NameItem = "Mediana",      PriceDelta = 0.00m,  IsDefault = true,  IdProductRecipe = null, SortOrder = 1 },
            new ProductOptionItem { IdProductOptionItem = 2, IdProductOptionGroup = 1, NameItem = "Grande",       PriceDelta = 2.00m,  IsDefault = false, IdProductRecipe = 6,    SortOrder = 2 },
            new ProductOptionItem { IdProductOptionItem = 3, IdProductOptionGroup = 2, NameItem = "Clásica",      PriceDelta = 0.00m,  IsDefault = true,  IdProductRecipe = null, SortOrder = 1 },
            new ProductOptionItem { IdProductOptionItem = 4, IdProductOptionGroup = 2, NameItem = "Delgada",      PriceDelta = 0.00m,  IsDefault = false, IdProductRecipe = null, SortOrder = 2 },
            new ProductOptionItem { IdProductOptionItem = 5, IdProductOptionGroup = 3, NameItem = "Pepperoni",    PriceDelta = 0.00m,  IsDefault = true,  IdProductRecipe = 4,    SortOrder = 1 },
            new ProductOptionItem { IdProductOptionItem = 6, IdProductOptionGroup = 3, NameItem = "Hawaiian",     PriceDelta = 0.00m,  IsDefault = false, IdProductRecipe = 5,    SortOrder = 2 },
            new ProductOptionItem { IdProductOptionItem = 7, IdProductOptionGroup = 4, NameItem = "Doble Queso",  PriceDelta = 0.75m,  IsDefault = false, IdProductRecipe = 7,    SortOrder = 1 }
        );
    }
}
