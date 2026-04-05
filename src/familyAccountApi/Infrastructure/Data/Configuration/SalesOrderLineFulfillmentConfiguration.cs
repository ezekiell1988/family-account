using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesOrderLineFulfillmentConfiguration : IEntityTypeConfiguration<SalesOrderLineFulfillment>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineFulfillment> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Detalle de cómo se cumple cada línea del pedido: con stock existente (FulfillmentType='Stock' → IdInventoryLot) o con producción planificada (FulfillmentType='Produccion' → IdProductionOrder). Una línea puede tener múltiples registros para mezclar stock y producción.");
            t.HasCheckConstraint("CK_salesOrderLineFulfillment_type", "fulfillmentType IN ('Stock', 'Produccion')");
            t.HasCheckConstraint("CK_salesOrderLineFulfillment_lot_or_order",
                "(fulfillmentType = 'Stock' AND idInventoryLot IS NOT NULL AND idProductionOrder IS NULL) OR " +
                "(fulfillmentType = 'Produccion' AND idProductionOrder IS NOT NULL AND idInventoryLot IS NULL)");
        });

        builder.HasKey(f => f.IdSalesOrderLineFulfillment);
        builder.Property(f => f.IdSalesOrderLineFulfillment)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(f => f.IdSalesOrderLine)
            .IsRequired()
            .HasComment("FK a la línea del pedido que se está cumpliendo.");

        builder.Property(f => f.FulfillmentType)
            .HasMaxLength(15)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Tipo: 'Stock' = se toma de un lote existente; 'Produccion' = se producirá contra esta línea.");

        builder.Property(f => f.IdInventoryLot)
            .HasComment("FK al lote de inventario asignado cuando FulfillmentType = 'Stock'.");

        builder.Property(f => f.IdProductionOrder)
            .HasComment("FK a la orden de producción cuando FulfillmentType = 'Produccion'.");

        builder.Property(f => f.QuantityBase)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Cantidad en unidad base asignada a este fulfillment.");

        builder.Property(f => f.UnitCost)
            .HasPrecision(18, 4)
            .HasComment("Snapshot del costo unitario al confirmar la factura final.");

        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación.");

        builder.HasIndex(f => f.IdSalesOrderLine)
            .HasDatabaseName("IX_salesOrderLineFulfillment_idSalesOrderLine");

        builder.HasIndex(f => f.IdInventoryLot)
            .HasFilter("[idInventoryLot] IS NOT NULL")
            .HasDatabaseName("IX_salesOrderLineFulfillment_idInventoryLot");

        builder.HasIndex(f => f.IdProductionOrder)
            .HasFilter("[idProductionOrder] IS NOT NULL")
            .HasDatabaseName("IX_salesOrderLineFulfillment_idProductionOrder");

        builder.HasOne(f => f.IdSalesOrderLineNavigation)
            .WithMany(sol => sol.SalesOrderLineFulfillments)
            .HasForeignKey(f => f.IdSalesOrderLine)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.IdInventoryLotNavigation)
            .WithMany()
            .HasForeignKey(f => f.IdInventoryLot)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.IdProductionOrderNavigation)
            .WithMany(po => po.SalesOrderLineFulfillments)
            .HasForeignKey(f => f.IdProductionOrder)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
