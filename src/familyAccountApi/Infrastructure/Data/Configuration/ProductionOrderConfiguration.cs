using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Orden de producción. IdSalesOrder = NULL → Modalidad A (producción para stock); IdSalesOrder NOT NULL → Modalidad B (contra pedido). Permite múltiples corridas de InventoryAdjustment tipo PRODUCCION bajo la misma orden.");
            t.HasCheckConstraint("CK_productionOrder_statusProductionOrder", "statusProductionOrder IN ('Borrador', 'Pendiente', 'EnProceso', 'Completado', 'Anulado')");
        });

        builder.HasKey(po => po.IdProductionOrder);
        builder.Property(po => po.IdProductionOrder)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la orden de producción.");

        builder.Property(po => po.IdFiscalPeriod)
            .IsRequired()
            .HasComment("FK al período fiscal al que corresponde la orden.");

        builder.Property(po => po.IdSalesOrder)
            .HasComment("FK al pedido de venta que origina esta orden. NULL = producción para stock (Modalidad A).");

        builder.Property(po => po.NumberProductionOrder)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Número correlativo de la orden (ej: OP-2026-0001).");

        builder.Property(po => po.DateOrder)
            .IsRequired()
            .HasComment("Fecha de creación de la orden de producción.");

        builder.Property(po => po.DateRequired)
            .HasComment("Fecha en que se necesita tener disponible lo producido. Nullable.");

        builder.Property(po => po.StatusProductionOrder)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Borrador")
            .HasComment("Estado: Borrador | Pendiente | EnProceso | Completado | Anulado.");

        builder.Property(po => po.DescriptionOrder)
            .HasMaxLength(500)
            .HasComment("Observaciones opcionales.");

        builder.Property(po => po.IdWarehouse)
            .HasComment("Bodega de producción: consumo de materias primas y entrada del producto terminado.");

        builder.Property(po => po.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");

        builder.HasIndex(po => po.NumberProductionOrder)
            .IsUnique()
            .HasDatabaseName("UQ_productionOrder_numberProductionOrder");

        builder.HasIndex(po => po.IdFiscalPeriod)
            .HasDatabaseName("IX_productionOrder_idFiscalPeriod");

        builder.HasIndex(po => po.IdSalesOrder)
            .HasFilter("[idSalesOrder] IS NOT NULL")
            .HasDatabaseName("IX_productionOrder_idSalesOrder");

        builder.HasIndex(po => po.IdWarehouse)
            .HasFilter("[idWarehouse] IS NOT NULL")
            .HasDatabaseName("IX_productionOrder_idWarehouse");

        builder.HasOne(po => po.IdFiscalPeriodNavigation)
            .WithMany()
            .HasForeignKey(po => po.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(po => po.IdSalesOrderNavigation)
            .WithMany(so => so.ProductionOrders)
            .HasForeignKey(po => po.IdSalesOrder)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(po => po.IdWarehouseNavigation)
            .WithMany()
            .HasForeignKey(po => po.IdWarehouse)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
