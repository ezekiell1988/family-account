using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class InventoryAdjustmentLineConfiguration : IEntityTypeConfiguration<InventoryAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustmentLine> builder)
    {
        builder.ToTable(t => t.HasComment("Líneas del ajuste de inventario. Cada línea referencia un lote específico: quantityDelta positivo = entrada, negativo = salida, cero = ajuste de costo puro. Si quantityDelta > 0, unitCostNew es requerido."));

        builder.HasKey(ial => ial.IdInventoryAdjustmentLine);
        builder.Property(ial => ial.IdInventoryAdjustmentLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea.");

        builder.Property(ial => ial.IdInventoryAdjustment)
            .IsRequired()
            .HasComment("FK al ajuste de inventario cabecera. Cascade delete.");

        builder.Property(ial => ial.IdInventoryLot)
            .IsRequired()
            .HasComment("FK al lote de inventario a ajustar. Para líneas positivas que crean un lote nuevo, se crea el lote primero.");

        builder.Property(ial => ial.QuantityDelta)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro (no mueve stock).");

        builder.Property(ial => ial.UnitCostNew)
            .HasPrecision(18, 6)
            .HasComment("Nuevo costo unitario para el lote. Requerido si quantityDelta > 0. Si informado: reemplaza inventoryLot.unitCost.");

        builder.Property(ial => ial.DescriptionLine)
            .HasMaxLength(500)
            .HasComment("Detalle por línea: insumo consumido, merma, motivo del ajuste, etc.");

        builder.HasIndex(ial => ial.IdInventoryAdjustment)
            .HasDatabaseName("IX_inventoryAdjustmentLine_idInventoryAdjustment");

        builder.HasOne(ial => ial.IdInventoryAdjustmentNavigation)
            .WithMany(ia => ia.InventoryAdjustmentLines)
            .HasForeignKey(ial => ial.IdInventoryAdjustment)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ial => ial.IdInventoryLotNavigation)
            .WithMany(il => il.AdjustmentLines)
            .HasForeignKey(ial => ial.IdInventoryLot)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
