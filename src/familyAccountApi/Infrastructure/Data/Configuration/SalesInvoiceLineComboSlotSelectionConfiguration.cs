using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceLineComboSlotSelectionConfiguration : IEntityTypeConfiguration<SalesInvoiceLineComboSlotSelection>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLineComboSlotSelection> builder)
    {
        builder.ToTable(t => t.HasComment("Snapshot inmutable de la selección por slot al generar la factura de venta."));

        builder.HasKey(s => s.IdSalesInvoiceLineComboSlotSelection);
        builder.Property(s => s.IdSalesInvoiceLineComboSlotSelection)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(s => s.IdSalesInvoiceLine)
            .IsRequired()
            .HasComment("FK a la línea de la factura (combo).");

        builder.Property(s => s.IdProductComboSlot)
            .IsRequired()
            .HasComment("FK al slot del combo.");

        builder.Property(s => s.IdProduct)
            .IsRequired()
            .HasComment("Producto elegido en el slot (snapshot al momento de facturar).");

        builder.Property(s => s.IdInventoryLot)
            .HasComment("Lote de producto terminado pre-asignado desde producción (nullable — slot sin receta lo omite).");

        // ── Índice único: un slot no puede aparecer dos veces por línea ──────
        builder.HasIndex(s => new { s.IdSalesInvoiceLine, s.IdProductComboSlot })
            .IsUnique()
            .HasDatabaseName("UQ_salesInvoiceLineComboSlotSelection_line_slot");

        // ── FK: SalesInvoiceLine ─────────────────────────────────────────────
        builder.HasOne(s => s.IdSalesInvoiceLineNavigation)
            .WithMany(l => l.ComboSlotSelections)
            .HasForeignKey(s => s.IdSalesInvoiceLine)
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

        // ── FK: InventoryLot (nullable) ──────────────────────────────────────
        builder.HasOne(s => s.IdInventoryLotNavigation)
            .WithMany()
            .HasForeignKey(s => s.IdInventoryLot)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
