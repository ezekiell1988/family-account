using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo interno de productos de la empresa. Un producto puede estar relacionado con uno o más SKUs escaneables (productProductSKU) y pertenecer a una o más categorías (productProductCategory)."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(p => p.IdProduct);
        builder.Property(p => p.IdProduct)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del producto.");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(p => p.CodeProduct)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código interno único del producto. Definido por la empresa, distinto al código de barras del fabricante.");

        builder.Property(p => p.NameProduct)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre interno del producto usado en el sistema.");

        // ── Índice único ─────────────────────────────────────
        builder.HasIndex(p => p.CodeProduct)
            .IsUnique()
            .HasDatabaseName("UQ_product_codeProduct");
    }
}
