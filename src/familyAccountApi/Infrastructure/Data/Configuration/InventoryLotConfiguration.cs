using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Registro de stock de inventario por producto y lote. Es la unidad mínima de trazabilidad. quantityAvailable nunca se edita directamente: solo se modifica al confirmar purchaseInvoice, salesInvoice o inventoryAdjustment. quantityReserved se incrementa al asignar un SalesOrderLineFulfillment tipo Stock y se decrementa al confirmar o eliminar el fulfillment.");
            t.HasCheckConstraint("CK_inventoryLot_sourceType", "sourceType IN ('Compra', 'Producción', 'Ajuste')");
            t.HasCheckConstraint("CK_inventoryLot_quantityAvailable", "quantityAvailable >= 0");
            t.HasCheckConstraint("CK_inventoryLot_quantityReserved", "quantityReserved >= 0");
            t.HasCheckConstraint("CK_inventoryLot_statusLot", "statusLot IN ('Disponible', 'Cuarentena', 'Bloqueado', 'Vencido')");
        });

        builder.HasKey(il => il.IdInventoryLot);
        builder.Property(il => il.IdInventoryLot)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del lote.");

        builder.Property(il => il.IdProduct)
            .IsRequired()
            .HasComment("FK al producto de este lote.");

        builder.Property(il => il.LotNumber)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasComment("Número de lote: '{idContact}-{numberInvoice}' para compras, código interno para producción, 'SYSTEM-{idInventoryAdjustment}' para ajustes. NULL si no aplica.");

        builder.Property(il => il.ExpirationDate)
            .HasComment("Fecha de vencimiento del lote. NULL para productos no perecederos.");

        builder.Property(il => il.UnitCost)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Costo unitario en unidad base al momento del ingreso del lote.");

        builder.Property(il => il.QuantityAvailable)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Stock disponible en unidad base. Solo se modifica al confirmar documentos. Nunca editable directamente.");

        builder.Property(il => il.QuantityReserved)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Stock reservado por SalesOrderLineFulfillment de tipo Stock pendientes de confirmar. Se incrementa al asignar un fulfillment y se decrementa al confirmar o eliminar el fulfillment. QuantityAvailableNet = QuantityAvailable - QuantityReserved.");

        builder.Property(il => il.StatusLot)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Disponible")
            .HasComment("Estado de calidad del lote: Disponible | Cuarentena | Bloqueado | Vencido. Solo los lotes Disponibles son seleccionables en FEFO.");

        builder.Property(il => il.SourceType)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Origen del lote: Compra | Producción | Ajuste.");

        builder.Property(il => il.IdPurchaseInvoice)
            .HasComment("FK a la factura de compra que originó este lote. Poblado si sourceType = 'Compra'.");

        builder.Property(il => il.IdInventoryAdjustment)
            .HasComment("FK al ajuste de inventario que originó este lote. Poblado si sourceType = 'Ajuste' o 'Producción' (V1).");

        builder.Property(il => il.IdWarehouse)
            .IsRequired()
            .HasDefaultValue(1)
            .HasComment("FK al almacén donde se encuentra este lote. Por defecto el almacén Principal (id=1).");

        builder.Property(il => il.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");

        // ── Índices ──────────────────────────────────────────
        builder.HasIndex(il => new { il.IdProduct, il.ExpirationDate })
            .HasDatabaseName("IX_inventoryLot_idProduct_expirationDate");

        builder.HasIndex(il => il.IdPurchaseInvoice)
            .HasFilter("[idPurchaseInvoice] IS NOT NULL")
            .HasDatabaseName("IX_inventoryLot_idPurchaseInvoice");

        builder.HasIndex(il => il.IdInventoryAdjustment)
            .HasFilter("[idInventoryAdjustment] IS NOT NULL")
            .HasDatabaseName("IX_inventoryLot_idInventoryAdjustment");

        // ── FK: Product ──────────────────────────────────────
        builder.HasOne(il => il.Product)
            .WithMany()
            .HasForeignKey(il => il.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: PurchaseInvoice ───────────────────────────────
        builder.HasOne(il => il.IdPurchaseInvoiceNavigation)
            .WithMany()
            .HasForeignKey(il => il.IdPurchaseInvoice)
            .OnDelete(DeleteBehavior.SetNull);

        // ── FK: InventoryAdjustment ───────────────────────────
        builder.HasOne(il => il.IdInventoryAdjustmentNavigation)
            .WithMany()
            .HasForeignKey(il => il.IdInventoryAdjustment)
            .OnDelete(DeleteBehavior.SetNull);

        // ── FK: Warehouse ─────────────────────────────────────
        builder.HasOne(il => il.Warehouse)
            .WithMany(w => w.InventoryLots)
            .HasForeignKey(il => il.IdWarehouse)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(il => il.IdWarehouse)
            .HasDatabaseName("IX_inventoryLot_idWarehouse");
    }
}
