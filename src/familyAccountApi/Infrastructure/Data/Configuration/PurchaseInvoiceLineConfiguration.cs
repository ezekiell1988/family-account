using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class PurchaseInvoiceLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLine>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceLine> builder)
    {
        builder.ToTable(t => t.HasComment("Líneas de la factura de compra. Cada línea representa un producto o servicio adquirido. Cuando idProduct está presente, al confirmar la factura se crea automáticamente un registro en inventoryLot y se recalcula product.averageCost."));

        builder.HasKey(pil => pil.IdPurchaseInvoiceLine);
        builder.Property(pil => pil.IdPurchaseInvoiceLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea de factura de compra.");

        builder.Property(pil => pil.IdPurchaseInvoice)
            .IsRequired()
            .HasComment("FK a la factura de compra cabecera. Cascade delete.");

        builder.Property(pil => pil.IdProduct)
            .HasComment("FK al producto. NULL para líneas de gasto sin producto (flete, servicios, etc.).");

        builder.Property(pil => pil.IdUnit)
            .HasComment("FK a la unidad de medida de la compra. Debe existir en productUnit para el idProduct con usedForPurchase=1. NULL si idProduct es NULL.");

        builder.Property(pil => pil.DescriptionLine)
            .HasMaxLength(500)
            .IsRequired()
            .HasComment("Descripción de la línea tal como aparece en la factura del proveedor.");

        builder.Property(pil => pil.Quantity)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Cantidad comprada en la unidad idUnit.");

        builder.Property(pil => pil.QuantityBase)
            .HasPrecision(18, 6)
            .HasComment("Cantidad en unidad base del producto: quantity × productUnit.conversionFactor. Calculado al confirmar la factura. No editable.");

        builder.Property(pil => pil.UnitPrice)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Precio unitario del producto o servicio en la unidad idUnit.");

        builder.Property(pil => pil.TaxPercent)
            .HasPrecision(5, 2)
            .IsRequired()
            .HasComment("Porcentaje de impuesto aplicado a la línea (ej: 13.00 para IVA 13%).");

        builder.Property(pil => pil.TotalLineAmount)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Total de la línea: quantity × unitPrice × (1 + taxPercent / 100).");

        builder.Property(pil => pil.LotNumber)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasComment("Número de lote del proveedor impreso en la etiqueta física del insumo. Pasa a inventoryLot.lotNumber al confirmar.");

        builder.Property(pil => pil.ExpirationDate)
            .HasComment("Fecha de vencimiento según la etiqueta del proveedor. Pasa a inventoryLot.expirationDate al confirmar. NULL para productos no perecederos.");

        builder.HasIndex(pil => pil.IdPurchaseInvoice)
            .HasDatabaseName("IX_purchaseInvoiceLine_idPurchaseInvoice");

        builder.HasIndex(pil => pil.IdProduct)
            .HasFilter("[idProduct] IS NOT NULL")
            .HasDatabaseName("IX_purchaseInvoiceLine_idProduct");

        builder.HasOne(pil => pil.IdPurchaseInvoiceNavigation)
            .WithMany(pi => pi.PurchaseInvoiceLines)
            .HasForeignKey(pil => pil.IdPurchaseInvoice)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pil => pil.IdProductNavigation)
            .WithMany()
            .HasForeignKey(pil => pil.IdProduct)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pil => pil.IdUnitNavigation)
            .WithMany()
            .HasForeignKey(pil => pil.IdUnit)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
