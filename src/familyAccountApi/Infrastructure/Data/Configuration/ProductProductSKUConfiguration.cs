using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductProductSKUConfiguration : IEntityTypeConfiguration<ProductProductSKU>
{
    public void Configure(EntityTypeBuilder<ProductProductSKU> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla de asociación muchos-a-muchos entre productos internos y SKUs de código de barras. Permite que un producto interno (product) esté vinculado a múltiples SKUs escaneables y que un mismo SKU pueda usarse en varios productos."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(pp => pp.IdProductProductSKU);
        builder.Property(pp => pp.IdProductProductSKU)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la asociación producto-SKU.");

        builder.Property(pp => pp.IdProduct)
            .HasComment("FK al producto interno.");

        builder.Property(pp => pp.IdProductSKU)
            .HasComment("FK al SKU de código de barras.");

        // ── Índice único compuesto ───────────────────────────
        builder.HasIndex(pp => new { pp.IdProduct, pp.IdProductSKU })
            .IsUnique()
            .HasDatabaseName("UQ_productProductSKU_idProduct_idProductSKU");

        // ── FK → Product ─────────────────────────────────────
        builder.HasOne(pp => pp.Product)
            .WithMany(p => p.ProductProductSKUs)
            .HasForeignKey(pp => pp.IdProduct)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK → ProductSKU ──────────────────────────────────
        builder.HasOne(pp => pp.ProductSKU)
            .WithMany(s => s.ProductProductSKUs)
            .HasForeignKey(pp => pp.IdProductSKU)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
