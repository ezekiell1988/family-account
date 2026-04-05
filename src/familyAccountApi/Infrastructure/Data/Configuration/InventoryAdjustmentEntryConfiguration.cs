using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class InventoryAdjustmentEntryConfiguration : IEntityTypeConfiguration<InventoryAdjustmentEntry>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustmentEntry> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla auxiliar N:M entre inventoryAdjustment y accountingEntry. Un ajuste puede vincularse a más de un asiento: el asiento inicial de confirmación y cualquier asiento de reversión posterior. Nunca se modifica un asiento confirmado; se agregan nuevas filas en esta tabla."));

        builder.HasKey(iae => iae.IdInventoryAdjustmentEntry);
        builder.Property(iae => iae.IdInventoryAdjustmentEntry)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del vínculo ajuste-asiento.");

        builder.Property(iae => iae.IdInventoryAdjustment)
            .IsRequired()
            .HasComment("FK al ajuste de inventario.");

        builder.Property(iae => iae.IdAccountingEntry)
            .IsRequired()
            .HasComment("FK al asiento contable vinculado al ajuste.");

        builder.HasIndex(iae => new { iae.IdInventoryAdjustment, iae.IdAccountingEntry })
            .IsUnique()
            .HasDatabaseName("UQ_inventoryAdjustmentEntry_idInventoryAdjustment_idAccountingEntry");

        builder.HasIndex(iae => iae.IdAccountingEntry)
            .HasDatabaseName("IX_inventoryAdjustmentEntry_idAccountingEntry");

        builder.HasOne(iae => iae.IdInventoryAdjustmentNavigation)
            .WithMany(ia => ia.InventoryAdjustmentEntries)
            .HasForeignKey(iae => iae.IdInventoryAdjustment)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(iae => iae.IdAccountingEntryNavigation)
            .WithMany()
            .HasForeignKey(iae => iae.IdAccountingEntry)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
