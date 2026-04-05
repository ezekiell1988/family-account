using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceLineBomDetailConfiguration : IEntityTypeConfiguration<SalesInvoiceLineBomDetail>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLineBomDetail> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Detalle de movimiento de inventario generado al confirmar una SalesInvoiceLine mediante " +
            "explosión BOM (receta activa — Opción 2B) o por slot de combo (Opción 3A). " +
            "Una línea puede originar N registros: uno por insumo de receta o por producto de slot. " +
            "IdProductRecipeLine = NULL indica reventa directa de slot o insumo extra no previsto en receta."));

        builder.HasKey(d => d.IdSalesInvoiceLineBomDetail);
        builder.Property(d => d.IdSalesInvoiceLineBomDetail)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del detalle BOM.");

        builder.Property(d => d.IdSalesInvoiceLine)
            .IsRequired()
            .HasComment("FK a la línea de factura de venta que originó este movimiento.");

        builder.Property(d => d.IdProductComboSlot)
            .HasComment("FK nullable al slot del combo. NULL si la línea no es un combo.");

        builder.Property(d => d.IdProductRecipeLine)
            .HasComment("FK nullable a la línea de receta. NULL si es reventa directa de slot o insumo extra.");

        builder.Property(d => d.IdProduct)
            .IsRequired()
            .HasComment("Snapshot del insumo o producto de slot descontado al confirmar.");

        builder.Property(d => d.IdInventoryLot)
            .IsRequired()
            .HasComment("Lote específico del que se descontó el stock (FEFO auto-asignado).");

        builder.Property(d => d.QuantityConsumed)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasComment("Cantidad descontada en unidad base del insumo/producto.");

        builder.Property(d => d.UnitCost)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Snapshot del costo unitario del lote al momento de confirmar la factura.");

        // ── FKs ──────────────────────────────────────────────────────────────
        builder.HasOne(d => d.IdSalesInvoiceLineNavigation)
            .WithMany(l => l.BomDetails)
            .HasForeignKey(d => d.IdSalesInvoiceLine)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.IdProductComboSlotNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdProductComboSlot)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.IdProductRecipeLineNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdProductRecipeLine)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.IdProductNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.IdInventoryLotNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdInventoryLot)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Índices ───────────────────────────────────────────────────────────
        builder.HasIndex(d => d.IdSalesInvoiceLine)
            .HasDatabaseName("IX_salesInvoiceLineBomDetail_idSalesInvoiceLine");

        builder.HasIndex(d => d.IdInventoryLot)
            .HasDatabaseName("IX_salesInvoiceLineBomDetail_idInventoryLot");

        builder.HasIndex(d => d.IdProductComboSlot)
            .HasFilter("[idProductComboSlot] IS NOT NULL")
            .HasDatabaseName("IX_salesInvoiceLineBomDetail_idProductComboSlot");

        builder.HasIndex(d => d.IdProductRecipeLine)
            .HasFilter("[idProductRecipeLine] IS NOT NULL")
            .HasDatabaseName("IX_salesInvoiceLineBomDetail_idProductRecipeLine");
    }
}
