using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductProductCategoryConfiguration : IEntityTypeConfiguration<ProductProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductProductCategory> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla de asociación muchos-a-muchos entre productos internos y categorías. Permite que un producto pertenezca a múltiples categorías y que una categoría agrupe múltiples productos. No se permite la misma combinación producto-categoría dos veces."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(pp => pp.IdProductProductCategory);
        builder.Property(pp => pp.IdProductProductCategory)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la asociación producto-categoría.");

        builder.Property(pp => pp.IdProduct)
            .HasComment("FK al producto interno.");

        builder.Property(pp => pp.IdProductCategory)
            .HasComment("FK a la categoría de producto.");

        // ── Índice único compuesto ───────────────────────────
        builder.HasIndex(pp => new { pp.IdProduct, pp.IdProductCategory })
            .IsUnique()
            .HasDatabaseName("UQ_productProductCategory_idProduct_idProductCategory");

        // ── FK → Product ─────────────────────────────────────
        builder.HasOne(pp => pp.Product)
            .WithMany(p => p.ProductProductCategories)
            .HasForeignKey(pp => pp.IdProduct)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK → ProductCategory ──────────────────────────────
        builder.HasOne(pp => pp.ProductCategory)
            .WithMany(c => c.ProductProductCategories)
            .HasForeignKey(pp => pp.IdProductCategory)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
