using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Pedido de un cliente externo. Modalidad B de producción: permite mezclar stock existente y órdenes de producción para cumplir el pedido. Flujo: Borrador → Confirmado → EnProduccion → Completado → Anulado.");
            t.HasCheckConstraint("CK_salesOrder_statusOrder", "statusOrder IN ('Borrador', 'Confirmado', 'EnProduccion', 'Completado', 'Anulado')");
        });

        builder.HasKey(so => so.IdSalesOrder);
        builder.Property(so => so.IdSalesOrder)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del pedido.");

        builder.Property(so => so.IdFiscalPeriod)
            .IsRequired()
            .HasComment("FK al período fiscal.");

        builder.Property(so => so.IdCurrency)
            .IsRequired()
            .HasComment("FK a la moneda del pedido.");

        builder.Property(so => so.IdContact)
            .IsRequired()
            .HasComment("FK al cliente que realiza el pedido.");

        builder.Property(so => so.IdPriceList)
            .HasComment("FK a la lista de precios vigente al crear el pedido. Sirve como referencia; el precio real queda en SalesOrderLine.UnitPrice.");

        builder.Property(so => so.NumberOrder)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Número correlativo del pedido (ej: PED-2026-0001).");

        builder.Property(so => so.DateOrder)
            .IsRequired()
            .HasComment("Fecha en que se ingresó el pedido.");

        builder.Property(so => so.DateDelivery)
            .HasComment("Fecha compromiso de entrega al cliente. Nullable.");

        builder.Property(so => so.SubTotalAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Subtotal sin impuesto.");

        builder.Property(so => so.TaxAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto total de impuesto.");

        builder.Property(so => so.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Total del pedido (subtotal + impuesto).");

        builder.Property(so => so.ExchangeRateValue)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Tipo de cambio al momento de crear el pedido.");

        builder.Property(so => so.StatusOrder)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Borrador")
            .HasComment("Estado del pedido: Borrador | Confirmado | EnProduccion | Completado | Anulado.");

        builder.Property(so => so.DescriptionOrder)
            .HasMaxLength(500)
            .HasComment("Observaciones opcionales del pedido.");

        builder.Property(so => so.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");

        builder.HasIndex(so => so.NumberOrder)
            .IsUnique()
            .HasDatabaseName("UQ_salesOrder_numberOrder");

        builder.HasIndex(so => so.IdContact)
            .HasDatabaseName("IX_salesOrder_idContact");

        builder.HasIndex(so => so.IdFiscalPeriod)
            .HasDatabaseName("IX_salesOrder_idFiscalPeriod");

        builder.HasIndex(so => so.IdPriceList)
            .HasFilter("[idPriceList] IS NOT NULL")
            .HasDatabaseName("IX_salesOrder_idPriceList");

        builder.HasOne(so => so.IdFiscalPeriodNavigation)
            .WithMany()
            .HasForeignKey(so => so.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(so => so.IdCurrencyNavigation)
            .WithMany()
            .HasForeignKey(so => so.IdCurrency)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(so => so.IdContactNavigation)
            .WithMany()
            .HasForeignKey(so => so.IdContact)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(so => so.IdPriceListNavigation)
            .WithMany(pl => pl.SalesOrders)
            .HasForeignKey(so => so.IdPriceList)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
