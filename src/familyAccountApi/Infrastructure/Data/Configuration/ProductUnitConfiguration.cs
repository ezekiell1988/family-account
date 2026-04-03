using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.ToTable(t => t.HasComment("Presentaciones de compra/venta de un producto con su factor de conversión a la unidad base. Reemplaza productSKU + productProductSKU. El campo codeBarcode permite escanear EAN para pre-llenar líneas de factura."));

        builder.HasKey(pu => pu.IdProductUnit);
        builder.Property(pu => pu.IdProductUnit)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la presentación del producto.");

        builder.Property(pu => pu.IdProduct)
            .IsRequired()
            .HasComment("FK al producto al que pertenece esta presentación.");

        builder.Property(pu => pu.IdUnit)
            .IsRequired()
            .HasComment("FK a la unidad de medida de esta presentación (ej: LATA400, BOT160, KG).");

        builder.Property(pu => pu.ConversionFactor)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Cuántas unidades base equivalen a 1 de esta presentación. La fila base siempre vale 1.000000.");

        builder.Property(pu => pu.IsBase)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Exactamente 1 registro por producto marca la unidad base (isBase=1, conversionFactor=1, idUnit = product.idUnit).");

        builder.Property(pu => pu.UsedForPurchase)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si esta presentación puede usarse en líneas de factura de compra.");

        builder.Property(pu => pu.UsedForSale)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si esta presentación puede usarse en líneas de factura de venta.");

        builder.Property(pu => pu.CodeBarcode)
            .HasMaxLength(48)
            .IsUnicode(false)
            .HasComment("Código de barras EAN-8, EAN-13 o UPC-A del empaque. NULL si no tiene barcode. Único en todo el sistema.");

        builder.Property(pu => pu.NamePresentation)
            .HasMaxLength(200)
            .HasComment("Nombre comercial del empaque tal como aparece en la etiqueta (ej: Cahuita Salsa Caribeña 160ml).");

        builder.Property(pu => pu.BrandPresentation)
            .HasMaxLength(100)
            .HasComment("Marca del fabricante del empaque (ej: Fiesta de Diablitos, Aroy-D).");

        // ── Índice único: un producto no puede tener dos filas con la misma unidad ──
        builder.HasIndex(pu => new { pu.IdProduct, pu.IdUnit })
            .IsUnique()
            .HasDatabaseName("UQ_productUnit_idProduct_idUnit");

        // ── Índice único filtrado: un EAN identifica exactamente 1 presentación ──
        builder.HasIndex(pu => pu.CodeBarcode)
            .IsUnique()
            .HasFilter("[codeBarcode] IS NOT NULL")
            .HasDatabaseName("UQ_productUnit_codeBarcode");

        // ── FK: Product ──────────────────────────────────────
        builder.HasOne(pu => pu.Product)
            .WithMany(p => p.ProductUnits)
            .HasForeignKey(pu => pu.IdProduct)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: UnitOfMeasure ────────────────────────────────
        builder.HasOne(pu => pu.UnitOfMeasure)
            .WithMany(u => u.ProductUnits)
            .HasForeignKey(pu => pu.IdUnit)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
