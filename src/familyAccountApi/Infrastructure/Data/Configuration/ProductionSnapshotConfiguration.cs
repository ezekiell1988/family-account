using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductionSnapshotConfiguration : IEntityTypeConfiguration<ProductionSnapshot>
{
    public void Configure(EntityTypeBuilder<ProductionSnapshot> builder)
    {
        builder.ToTable(t => t.HasComment("Copia de la receta usada al confirmar un ajuste de producción. Registra la cantidad calculada (teórica) y la real producida para permitir ajustar recetas a lo largo del tiempo."));

        builder.HasKey(ps => ps.IdProductionSnapshot);
        builder.Property(ps => ps.IdProductionSnapshot)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del snapshot de producción.");

        builder.Property(ps => ps.IdInventoryAdjustment)
            .IsRequired()
            .HasComment("FK 1:1 al ajuste de inventario de tipo PRODUCCION.");

        builder.Property(ps => ps.IdProductRecipe)
            .IsRequired()
            .HasComment("FK a la receta vigente al momento de confirmar la producción.");

        builder.Property(ps => ps.QuantityCalculated)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasComment("Cantidad teórica del producto final según la receta (ProductRecipe.QuantityOutput al confirmar).");

        builder.Property(ps => ps.QuantityReal)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasComment("Cantidad real producida físicamente en esta corrida.");

        builder.Property(ps => ps.CreatedAt)
            .IsRequired()
            .HasComment("Fecha y hora UTC en que se creó el snapshot.");

        // ── Relaciones ──────────────────────────────────────
        builder.HasOne(ps => ps.IdInventoryAdjustmentNavigation)
            .WithMany()
            .HasForeignKey(ps => ps.IdInventoryAdjustment)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ps => ps.IdProductRecipeNavigation)
            .WithMany()
            .HasForeignKey(ps => ps.IdProductRecipe)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Índices ─────────────────────────────────────────
        // 1:1 con inventoryAdjustment
        builder.HasIndex(ps => ps.IdInventoryAdjustment)
            .IsUnique()
            .HasDatabaseName("UQ_productionSnapshot_idInventoryAdjustment");

        builder.HasIndex(ps => ps.IdProductRecipe)
            .HasDatabaseName("IX_productionSnapshot_idProductRecipe");
    }
}
