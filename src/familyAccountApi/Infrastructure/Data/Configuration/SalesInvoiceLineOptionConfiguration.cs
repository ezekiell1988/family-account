using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceLineOptionConfiguration : IEntityTypeConfiguration<SalesInvoiceLineOption>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLineOption> builder)
    {
        builder.ToTable(t => t.HasComment("Opciones configurables copiadas desde el pedido a la factura de venta."));

        builder.HasKey(o => o.IdSalesInvoiceLineOption);
        builder.Property(o => o.IdSalesInvoiceLineOption)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(o => o.IdSalesInvoiceLine)
            .IsRequired()
            .HasComment("FK a la línea de la factura.");

        builder.Property(o => o.IdProductOptionItem)
            .IsRequired()
            .HasComment("FK al ítem de opción.");

        builder.Property(o => o.Quantity)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasDefaultValue(1m)
            .HasComment("Cantidad de este option item aplicado a la línea.");

        // ── FK: SalesInvoiceLine ─────────────────────────────────────────────
        builder.HasOne(o => o.IdSalesInvoiceLineNavigation)
            .WithMany(l => l.SalesInvoiceLineOptions)
            .HasForeignKey(o => o.IdSalesInvoiceLine)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: ProductOptionItem ────────────────────────────────────────────
        builder.HasOne(o => o.IdProductOptionItemNavigation)
            .WithMany()
            .HasForeignKey(o => o.IdProductOptionItem)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
