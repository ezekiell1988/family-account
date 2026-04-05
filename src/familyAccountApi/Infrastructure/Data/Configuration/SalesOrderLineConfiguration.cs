using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable(t => t.HasComment("Línea del pedido de venta. UnitPrice es snapshot del precio de la lista vigente al crear el pedido."));

        builder.HasKey(sol => sol.IdSalesOrderLine);
        builder.Property(sol => sol.IdSalesOrderLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea.");

        builder.Property(sol => sol.IdSalesOrder)
            .IsRequired()
            .HasComment("FK al pedido de venta cabecera.");

        builder.Property(sol => sol.IdProduct)
            .IsRequired()
            .HasComment("FK al producto solicitado.");

        builder.Property(sol => sol.IdProductUnit)
            .IsRequired()
            .HasComment("FK a la presentación (unidad de venta) en que se pide el producto.");

        builder.Property(sol => sol.IdPriceListItem)
            .HasComment("FK al ítem de lista de precios del que se tomó el precio. NULL si se ingresó manualmente.");

        builder.Property(sol => sol.Quantity)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Cantidad en la unidad de venta solicitada.");

        builder.Property(sol => sol.QuantityBase)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Cantidad en unidad base, calculada × ConversionFactor al confirmar.");

        builder.Property(sol => sol.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Snapshot del precio unitario al crear el pedido. No cambia aunque la lista de precios se actualice.");

        builder.Property(sol => sol.TaxPercent)
            .HasPrecision(5, 2)
            .IsRequired()
            .HasComment("Porcentaje de impuesto al momento del pedido (ej: 13.00).");

        builder.Property(sol => sol.TotalLineAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Total de la línea (Quantity × UnitPrice × (1 + TaxPercent / 100)).");

        builder.Property(sol => sol.DescriptionLine)
            .HasMaxLength(500)
            .HasComment("Descripción o nota adicional de la línea.");

        builder.HasIndex(sol => sol.IdSalesOrder)
            .HasDatabaseName("IX_salesOrderLine_idSalesOrder");

        builder.HasIndex(sol => sol.IdPriceListItem)
            .HasFilter("[idPriceListItem] IS NOT NULL")
            .HasDatabaseName("IX_salesOrderLine_idPriceListItem");

        builder.HasOne(sol => sol.IdSalesOrderNavigation)
            .WithMany(so => so.SalesOrderLines)
            .HasForeignKey(sol => sol.IdSalesOrder)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sol => sol.IdProductNavigation)
            .WithMany()
            .HasForeignKey(sol => sol.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sol => sol.IdProductUnitNavigation)
            .WithMany()
            .HasForeignKey(sol => sol.IdProductUnit)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sol => sol.IdPriceListItemNavigation)
            .WithMany()
            .HasForeignKey(sol => sol.IdPriceListItem)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
