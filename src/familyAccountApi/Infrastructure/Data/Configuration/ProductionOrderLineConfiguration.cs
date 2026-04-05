using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductionOrderLineConfiguration : IEntityTypeConfiguration<ProductionOrderLine>
{
    public void Configure(EntityTypeBuilder<ProductionOrderLine> builder)
    {
        builder.ToTable(t => t.HasComment("Línea de orden de producción. Registra qué producto producir, cuánto se requiere (QuantityRequired) y cuánto se ha producido acumulado (QuantityProduced). Vinculada opcionalmente a la línea del pedido de origen para calcular margen por pedido."));

        builder.HasKey(pol => pol.IdProductionOrderLine);
        builder.Property(pol => pol.IdProductionOrderLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea.");

        builder.Property(pol => pol.IdProductionOrder)
            .IsRequired()
            .HasComment("FK a la orden de producción cabecera.");

        builder.Property(pol => pol.IdProduct)
            .IsRequired()
            .HasComment("FK al producto final a producir en esta línea.");

        builder.Property(pol => pol.IdProductUnit)
            .IsRequired()
            .HasComment("FK a la unidad de producción del producto.");

        builder.Property(pol => pol.IdSalesOrderLine)
            .HasComment("FK opcional a la línea del pedido de venta que originó esta línea de producción.");

        builder.Property(pol => pol.QuantityRequired)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Cantidad total comprometida en unidad base que se debe producir para cubrir el pedido.");

        builder.Property(pol => pol.QuantityProduced)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Acumulado producido. Se incrementa cada vez que se confirma un InventoryAdjustment vinculado a esta orden.");

        builder.Property(pol => pol.DescriptionLine)
            .HasMaxLength(500)
            .HasComment("Nota adicional opcional de la línea.");

        builder.HasIndex(pol => pol.IdProductionOrder)
            .HasDatabaseName("IX_productionOrderLine_idProductionOrder");

        builder.HasIndex(pol => pol.IdSalesOrderLine)
            .HasFilter("[idSalesOrderLine] IS NOT NULL")
            .HasDatabaseName("IX_productionOrderLine_idSalesOrderLine");

        builder.HasOne(pol => pol.IdProductionOrderNavigation)
            .WithMany(po => po.ProductionOrderLines)
            .HasForeignKey(pol => pol.IdProductionOrder)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pol => pol.IdProductNavigation)
            .WithMany()
            .HasForeignKey(pol => pol.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pol => pol.IdProductUnitNavigation)
            .WithMany()
            .HasForeignKey(pol => pol.IdProductUnit)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pol => pol.IdSalesOrderLineNavigation)
            .WithMany()
            .HasForeignKey(pol => pol.IdSalesOrderLine)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
