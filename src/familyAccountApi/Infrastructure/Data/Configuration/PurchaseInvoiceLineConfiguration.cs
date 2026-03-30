using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class PurchaseInvoiceLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLine>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceLine> builder)
    {
        builder.ToTable(t => t.HasComment("Líneas de la factura de compra. Cada línea representa un producto o servicio adquirido. Cuando IdProductSKU está presente la cadena productSKU → product → productAccount genera automáticamente las líneas DR del asiento contable."));

        builder.HasKey(pil => pil.IdPurchaseInvoiceLine);
        builder.Property(pil => pil.IdPurchaseInvoiceLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea de factura de compra.");

        builder.Property(pil => pil.IdPurchaseInvoice)
            .IsRequired()
            .HasComment("FK a la factura de compra cabecera. Cascade delete.");

        builder.Property(pil => pil.IdProductSKU)
            .HasComment("FK opcional al SKU del producto escaneado. Nullable hasta que se implemente el catálogo completo de productos.");

        builder.Property(pil => pil.DescriptionLine)
            .HasMaxLength(300)
            .IsRequired()
            .HasComment("Descripción de la línea tal como aparece en la factura del proveedor.");

        builder.Property(pil => pil.Quantity)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Cantidad comprada.");

        builder.Property(pil => pil.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Precio unitario del producto o servicio.");

        builder.Property(pil => pil.TaxPercent)
            .HasPrecision(5, 2)
            .IsRequired()
            .HasComment("Porcentaje de impuesto aplicado a la línea (ej. 13.00 para IVA 13%).");

        builder.Property(pil => pil.TotalLineAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Total de la línea calculado: Quantity * UnitPrice * (1 + TaxPercent / 100).");

        builder.HasIndex(pil => pil.IdPurchaseInvoice)
            .HasDatabaseName("IX_purchaseInvoiceLine_idPurchaseInvoice");

        builder.HasIndex(pil => pil.IdProductSKU)
            .HasDatabaseName("IX_purchaseInvoiceLine_idProductSKU")
            .HasFilter("[idProductSKU] IS NOT NULL");

        builder.HasOne(pil => pil.IdPurchaseInvoiceNavigation)
            .WithMany(pi => pi.PurchaseInvoiceLines)
            .HasForeignKey(pil => pil.IdPurchaseInvoice)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pil => pil.IdProductSKUNavigation)
            .WithMany()
            .HasForeignKey(pil => pil.IdProductSKU)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
