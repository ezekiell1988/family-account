using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo de categorías de productos. Permite agrupar y clasificar los productos internos para facilitar su búsqueda y organización. Un producto puede pertenecer a múltiples categorías a través de productProductCategory."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(c => c.IdProductCategory);
        builder.Property(c => c.IdProductCategory)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la categoría de producto.");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(c => c.NameProductCategory)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Nombre descriptivo de la categoría. Ej: 'Lácteos', 'Limpieza', 'Bebidas'.");
    }
}
