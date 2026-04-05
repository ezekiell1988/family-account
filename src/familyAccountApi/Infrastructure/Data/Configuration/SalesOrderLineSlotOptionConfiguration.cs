using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesOrderLineSlotOptionConfiguration : IEntityTypeConfiguration<SalesOrderLineSlotOption>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineSlotOption> builder)
    {
        builder.ToTable(t => t.HasComment("Opciones elegidas dentro de cada selección de slot (incluye presets copiados y opciones libres del cliente)."));

        builder.HasKey(o => o.IdSalesOrderLineSlotOption);
        builder.Property(o => o.IdSalesOrderLineSlotOption)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(o => o.IdSalesOrderLineComboSlotSelection)
            .IsRequired()
            .HasComment("FK a la selección de slot de la línea del pedido.");

        builder.Property(o => o.IdProductOptionItem)
            .IsRequired()
            .HasComment("FK al ítem de opción elegido dentro del slot.");

        builder.Property(o => o.Quantity)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasDefaultValue(1m)
            .HasComment("Cantidad de este option item aplicado al slot (por defecto 1).");

        builder.Property(o => o.IsPreset)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("true = opción copiada automáticamente del preset del slot; false = elegida libremente por el cliente.");

        // ── Índice único: sin duplicados por selección de slot ───────────────
        builder.HasIndex(o => new { o.IdSalesOrderLineComboSlotSelection, o.IdProductOptionItem })
            .IsUnique()
            .HasDatabaseName("UQ_salesOrderLineSlotOption_selection_item");

        // ── FK: SalesOrderLineComboSlotSelection ─────────────────────────────
        builder.HasOne(o => o.IdSalesOrderLineComboSlotSelectionNavigation)
            .WithMany(s => s.SalesOrderLineSlotOptions)
            .HasForeignKey(o => o.IdSalesOrderLineComboSlotSelection)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: ProductOptionItem ────────────────────────────────────────────
        builder.HasOne(o => o.IdProductOptionItemNavigation)
            .WithMany()
            .HasForeignKey(o => o.IdProductOptionItem)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
