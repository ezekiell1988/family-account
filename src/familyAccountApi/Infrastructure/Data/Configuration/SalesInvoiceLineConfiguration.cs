using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceLineConfiguration : IEntityTypeConfiguration<SalesInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLine> builder)
    {
        builder.ToTable(t => t.HasComment("Línea de la factura de venta. IdInventoryLot es obligatorio para productos con stock; se descuenta al confirmar."));

        builder.HasKey(sil => sil.IdSalesInvoiceLine);
        builder.Property(sil => sil.IdSalesInvoiceLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea.");

        builder.Property(sil => sil.DescriptionLine)
            .HasMaxLength(300)
            .IsRequired()
            .HasComment("Descripción del producto o servicio de la línea.");

        builder.Property(sil => sil.Quantity)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Cantidad en la unidad de venta seleccionada.");

        builder.Property(sil => sil.QuantityBase)
            .HasPrecision(18, 6)
            .HasComment("Cantidad en unidad base, calculada al confirmar: Quantity × ConversionFactor.");

        builder.Property(sil => sil.UnitPrice)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Precio unitario de venta en la presentación seleccionada.");

        builder.Property(sil => sil.UnitCost)
            .HasPrecision(18, 6)
            .HasComment("Costo unitario snapshot del lote en el momento de confirmar (para calcular COGS).");

        builder.Property(sil => sil.TaxPercent)
            .HasPrecision(5, 2)
            .IsRequired()
            .HasComment("Porcentaje de impuesto aplicado a esta línea.");

        builder.Property(sil => sil.TotalLineAmount)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasComment("Total de la línea incluyendo impuesto.");

        builder.HasOne(sil => sil.IdSalesInvoiceNavigation)
            .WithMany(si => si.SalesInvoiceLines)
            .HasForeignKey(sil => sil.IdSalesInvoice)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sil => sil.IdProductNavigation)
            .WithMany()
            .HasForeignKey(sil => sil.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sil => sil.IdUnitNavigation)
            .WithMany()
            .HasForeignKey(sil => sil.IdUnit)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sil => sil.IdInventoryLotNavigation)
            .WithMany()
            .HasForeignKey(sil => sil.IdInventoryLot)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
