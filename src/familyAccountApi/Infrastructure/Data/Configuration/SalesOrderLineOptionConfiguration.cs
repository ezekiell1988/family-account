using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesOrderLineOptionConfiguration : IEntityTypeConfiguration<SalesOrderLineOption>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineOption> builder)
    {
        builder.ToTable(t => t.HasComment("Opciones configurables seleccionadas en una línea de pedido (ej: masa delgada, extra queso)."));

        builder.HasKey(o => o.IdSalesOrderLineOption);
        builder.Property(o => o.IdSalesOrderLineOption)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(o => o.IdSalesOrderLine)
            .IsRequired()
            .HasComment("FK a la línea del pedido.");

        builder.Property(o => o.IdProductOptionItem)
            .IsRequired()
            .HasComment("FK al ítem de opción seleccionado.");

        builder.Property(o => o.Quantity)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasDefaultValue(1m)
            .HasComment("Cantidad de este option item aplicado a la línea (por defecto 1).");

        // ── FK: SalesOrderLine ───────────────────────────────────────────────
        builder.HasOne(o => o.IdSalesOrderLineNavigation)
            .WithMany(l => l.SalesOrderLineOptions)
            .HasForeignKey(o => o.IdSalesOrderLine)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: ProductOptionItem ────────────────────────────────────────────
        builder.HasOne(o => o.IdProductOptionItemNavigation)
            .WithMany()
            .HasForeignKey(o => o.IdProductOptionItem)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
