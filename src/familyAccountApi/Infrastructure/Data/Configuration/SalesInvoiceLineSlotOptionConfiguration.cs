using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceLineSlotOptionConfiguration : IEntityTypeConfiguration<SalesInvoiceLineSlotOption>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLineSlotOption> builder)
    {
        builder.ToTable(t => t.HasComment("Opciones del slot incluidas en la factura (snapshot al copiar desde el pedido)."));

        builder.HasKey(o => o.IdSalesInvoiceLineSlotOption);
        builder.Property(o => o.IdSalesInvoiceLineSlotOption)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(o => o.IdSalesInvoiceLineComboSlotSelection)
            .IsRequired()
            .HasComment("FK a la selección de slot de la línea de factura.");

        builder.Property(o => o.IdProductOptionItem)
            .IsRequired()
            .HasComment("FK al ítem de opción del slot.");

        builder.Property(o => o.Quantity)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasDefaultValue(1m)
            .HasComment("Cantidad de este option item aplicado al slot.");

        builder.Property(o => o.IsPreset)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("true = opción copiada automáticamente del preset del slot; false = elegida libremente por el cliente.");

        // ── Índice único: sin duplicados por selección de slot ───────────────
        builder.HasIndex(o => new { o.IdSalesInvoiceLineComboSlotSelection, o.IdProductOptionItem })
            .IsUnique()
            .HasDatabaseName("UQ_salesInvoiceLineSlotOption_selection_item");

        // ── FK: SalesInvoiceLineComboSlotSelection ────────────────────────────
        builder.HasOne(o => o.IdSalesInvoiceLineComboSlotSelectionNavigation)
            .WithMany(s => s.SalesInvoiceLineSlotOptions)
            .HasForeignKey(o => o.IdSalesInvoiceLineComboSlotSelection)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: ProductOptionItem ────────────────────────────────────────────
        builder.HasOne(o => o.IdProductOptionItemNavigation)
            .WithMany()
            .HasForeignKey(o => o.IdProductOptionItem)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
