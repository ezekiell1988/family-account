using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductSKUConfiguration : IEntityTypeConfiguration<ProductSKU>
{
    public void Configure(EntityTypeBuilder<ProductSKU> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo de SKUs de productos identificados por código de barras. Un SKU representa la unidad comercial exacta de un producto (marca + contenido + presentación). Múltiples productos internos pueden referenciar el mismo SKU a través de productProductSKU."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(p => p.IdProductSKU);
        builder.Property(p => p.IdProductSKU)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del SKU.");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(p => p.CodeProductSKU)
            .HasMaxLength(48)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código de barras del producto. Soporta EAN-8 (8 dígitos), EAN-13 (13 dígitos), UPC-A (12 dígitos) y otros formatos de hasta 48 caracteres.");

        builder.Property(p => p.NameProductSKU)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre completo del producto tal como aparece en el empaque.");

        // ── Campos opcionales ───────────────────────────────
        builder.Property(p => p.BrandProductSKU)
            .HasMaxLength(100)
            .HasComment("Marca o fabricante del producto. Ej: 'Nestlé', 'Unilever'.");

        builder.Property(p => p.DescriptionProductSKU)
            .HasMaxLength(500)
            .HasComment("Descripción detallada del producto: ingredientes, características, uso recomendado.");

        builder.Property(p => p.NetContent)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasComment("Contenido neto del producto con su unidad de medida. Ej: '500ml', '1kg', '12 unidades'.");

        // ── Índice único ─────────────────────────────────────
        builder.HasIndex(p => p.CodeProductSKU)
            .IsUnique()
            .HasDatabaseName("UQ_productSKU_codeProductSKU");
    }
}
