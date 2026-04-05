using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductRecipeLineConfiguration : IEntityTypeConfiguration<ProductRecipeLine>
{
    public void Configure(EntityTypeBuilder<ProductRecipeLine> builder)
    {
        builder.ToTable(t => t.HasComment("Ingredientes de una receta de producción. Cada línea es un insumo con su cantidad en unidad base. idProductInput no puede ser igual al idProductOutput de la receta padre."));

        builder.HasKey(rl => rl.IdProductRecipeLine);
        builder.Property(rl => rl.IdProductRecipeLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea de receta.");

        builder.Property(rl => rl.IdProductRecipe)
            .IsRequired()
            .HasComment("FK a la receta cabecera. Cascade delete.");

        builder.Property(rl => rl.IdProductInput)
            .IsRequired()
            .HasComment("FK al producto insumo. No puede ser igual al idProductOutput de la receta.");

        builder.Property(rl => rl.QuantityInput)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasComment("Cantidad requerida del insumo en su unidad base por cada corrida de la receta.");

        builder.Property(rl => rl.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Orden de visualización de los ingredientes.");

        builder.HasOne(rl => rl.IdProductRecipeNavigation)
            .WithMany(r => r.ProductRecipeLines)
            .HasForeignKey(rl => rl.IdProductRecipe)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rl => rl.IdProductInputNavigation)
            .WithMany()
            .HasForeignKey(rl => rl.IdProductInput)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rl => rl.IdProductRecipe)
            .HasDatabaseName("IX_productRecipeLine_idProductRecipe");

        // ── Seed: líneas de receta demo ──────────────────────────────────────
        // Receta 1 — Chile Embotellado (produce 1 frasco de 250ml)
        //   200g chile (0.2 KG) + 50ml vinagre (0.05 LTR) + 5g sal (0.005 KG) + 1 frasco
        // Receta 2 — Hot Dog (produce 1 hot dog)
        //   1 pan + 1 salchicha + 15ml mostaza + 20ml catsup
        // Receta 3 — Base Pizza (produce 1 pizza mediana)
        //   400g harina (0.4 KG) + 250ml agua (0.25 LTR) + 5g levadura + 30ml aceite + 100ml salsa + 150g mozzarella (0.15 KG)
        // Receta 4 — Opción Pepperoni: 100g pepperoni (0.1 KG)
        // Receta 5 — Opción Hawaiian: 80g piña (0.08 KG) + 80g jamón (0.08 KG)
        // Receta 6 — Opción Grande: +150g harina (0.15 KG) + +80ml agua (0.08 LTR)
        // Receta 7 — Opción Doble Queso: +50g mozzarella (0.05 KG)
        builder.HasData(
            // Receta 1: Chile Embotellado
            new ProductRecipeLine { IdProductRecipeLine =  1, IdProductRecipe = 1, IdProductInput =  2, QuantityInput = 0.2000m,  SortOrder = 1 },  // Chile Seco
            new ProductRecipeLine { IdProductRecipeLine =  2, IdProductRecipe = 1, IdProductInput =  3, QuantityInput = 0.0500m,  SortOrder = 2 },  // Vinagre
            new ProductRecipeLine { IdProductRecipeLine =  3, IdProductRecipe = 1, IdProductInput =  4, QuantityInput = 0.0050m,  SortOrder = 3 },  // Sal
            new ProductRecipeLine { IdProductRecipeLine =  4, IdProductRecipe = 1, IdProductInput =  5, QuantityInput = 1.0000m,  SortOrder = 4 },  // Frasco

            // Receta 2: Hot Dog
            new ProductRecipeLine { IdProductRecipeLine =  5, IdProductRecipe = 2, IdProductInput =  7, QuantityInput = 1.0000m,  SortOrder = 1 },  // Pan de Hot Dog
            new ProductRecipeLine { IdProductRecipeLine =  6, IdProductRecipe = 2, IdProductInput =  8, QuantityInput = 1.0000m,  SortOrder = 2 },  // Salchicha
            new ProductRecipeLine { IdProductRecipeLine =  7, IdProductRecipe = 2, IdProductInput =  9, QuantityInput = 15.0000m, SortOrder = 3 },  // Mostaza (ML)
            new ProductRecipeLine { IdProductRecipeLine =  8, IdProductRecipe = 2, IdProductInput = 10, QuantityInput = 20.0000m, SortOrder = 4 },  // Catsup (ML)

            // Receta 3: Base Pizza
            new ProductRecipeLine { IdProductRecipeLine =  9, IdProductRecipe = 3, IdProductInput = 18, QuantityInput = 0.4000m,  SortOrder = 1 },  // Harina de Trigo
            new ProductRecipeLine { IdProductRecipeLine = 10, IdProductRecipe = 3, IdProductInput = 19, QuantityInput = 0.2500m,  SortOrder = 2 },  // Agua
            new ProductRecipeLine { IdProductRecipeLine = 11, IdProductRecipe = 3, IdProductInput = 20, QuantityInput = 5.0000m,  SortOrder = 3 },  // Levadura (GR)
            new ProductRecipeLine { IdProductRecipeLine = 12, IdProductRecipe = 3, IdProductInput = 21, QuantityInput = 30.0000m, SortOrder = 4 },  // Aceite (ML)
            new ProductRecipeLine { IdProductRecipeLine = 13, IdProductRecipe = 3, IdProductInput = 22, QuantityInput = 100.000m, SortOrder = 5 },  // Salsa de Tomate (ML)
            new ProductRecipeLine { IdProductRecipeLine = 14, IdProductRecipe = 3, IdProductInput = 23, QuantityInput = 0.1500m,  SortOrder = 6 },  // Queso Mozzarella

            // Receta 4: Opción Pepperoni
            new ProductRecipeLine { IdProductRecipeLine = 15, IdProductRecipe = 4, IdProductInput = 24, QuantityInput = 0.1000m,  SortOrder = 1 },  // Pepperoni

            // Receta 5: Opción Hawaiian
            new ProductRecipeLine { IdProductRecipeLine = 16, IdProductRecipe = 5, IdProductInput = 25, QuantityInput = 0.0800m,  SortOrder = 1 },  // Piña
            new ProductRecipeLine { IdProductRecipeLine = 17, IdProductRecipe = 5, IdProductInput = 26, QuantityInput = 0.0800m,  SortOrder = 2 },  // Jamón

            // Receta 6: Opción Tamaño Grande (ingredientes extra sobre la base)
            new ProductRecipeLine { IdProductRecipeLine = 18, IdProductRecipe = 6, IdProductInput = 18, QuantityInput = 0.1500m,  SortOrder = 1 },  // Harina extra
            new ProductRecipeLine { IdProductRecipeLine = 19, IdProductRecipe = 6, IdProductInput = 19, QuantityInput = 0.0800m,  SortOrder = 2 },  // Agua extra

            // Receta 7: Opción Doble Queso
            new ProductRecipeLine { IdProductRecipeLine = 20, IdProductRecipe = 7, IdProductInput = 23, QuantityInput = 0.0500m,  SortOrder = 1 }   // Mozzarella extra
        );
    }
}
