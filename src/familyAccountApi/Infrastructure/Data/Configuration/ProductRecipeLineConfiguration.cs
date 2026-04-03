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
    }
}
