using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesOrderLineComboSlotSelectionConfiguration : IEntityTypeConfiguration<SalesOrderLineComboSlotSelection>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineComboSlotSelection> builder)
    {
        builder.ToTable(t => t.HasComment("Selección del cliente para cada slot del combo en una línea de pedido."));

        builder.HasKey(s => s.IdSalesOrderLineComboSlotSelection);
        builder.Property(s => s.IdSalesOrderLineComboSlotSelection)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(s => s.IdSalesOrderLine)
            .IsRequired()
            .HasComment("FK a la línea del pedido (combo).");

        builder.Property(s => s.IdProductComboSlot)
            .IsRequired()
            .HasComment("FK al slot del combo configurado.");

        builder.Property(s => s.IdProduct)
            .IsRequired()
            .HasComment("Producto elegido por el cliente en este slot.");

        // ── Índice único: un slot no puede seleccionarse dos veces por línea ─
        builder.HasIndex(s => new { s.IdSalesOrderLine, s.IdProductComboSlot })
            .IsUnique()
            .HasDatabaseName("UQ_salesOrderLineComboSlotSelection_line_slot");

        // ── FK: SalesOrderLine ───────────────────────────────────────────────
        builder.HasOne(s => s.IdSalesOrderLineNavigation)
            .WithMany(l => l.ComboSlotSelections)
            .HasForeignKey(s => s.IdSalesOrderLine)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: ProductComboSlot ─────────────────────────────────────────────
        builder.HasOne(s => s.IdProductComboSlotNavigation)
            .WithMany()
            .HasForeignKey(s => s.IdProductComboSlot)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: Product ──────────────────────────────────────────────────────
        builder.HasOne(s => s.IdProductNavigation)
            .WithMany()
            .HasForeignKey(s => s.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
