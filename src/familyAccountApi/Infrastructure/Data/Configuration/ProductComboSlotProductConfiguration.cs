using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductComboSlotProductConfiguration : IEntityTypeConfiguration<ProductComboSlotProduct>
{
    public void Configure(EntityTypeBuilder<ProductComboSlotProduct> builder)
    {
        builder.ToTable(t => t.HasComment("Productos permitidos en cada slot de un combo. El cliente elige uno de esta lista al armar el pedido."));

        builder.HasKey(sp => sp.IdProductComboSlotProduct);
        builder.Property(sp => sp.IdProductComboSlotProduct)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(sp => sp.IdProductComboSlot)
            .IsRequired()
            .HasComment("FK al slot del combo al que pertenece esta opción.");

        builder.Property(sp => sp.IdProduct)
            .IsRequired()
            .HasComment("FK al producto permitido en este slot.");

        builder.Property(sp => sp.PriceAdjustment)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Ajuste adicional al precio del combo por elegir este producto en el slot.");

        builder.Property(sp => sp.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Orden de presentación dentro del slot.");

        // ── Índice único: un producto no puede repetirse en el mismo slot ────
        builder.HasIndex(sp => new { sp.IdProductComboSlot, sp.IdProduct })
            .IsUnique()
            .HasDatabaseName("UQ_productComboSlotProduct_idSlot_idProduct");

        // ── FK: ProductComboSlot ─────────────────────────────────────────────
        builder.HasOne(sp => sp.IdProductComboSlotNavigation)
            .WithMany(s => s.ProductComboSlotProducts)
            .HasForeignKey(sp => sp.IdProductComboSlot)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: Product ──────────────────────────────────────────────────────
        builder.HasOne(sp => sp.IdProductNavigation)
            .WithMany()
            .HasForeignKey(sp => sp.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
