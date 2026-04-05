using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class InventoryAdjustmentLineConfiguration : IEntityTypeConfiguration<InventoryAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustmentLine> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Líneas del ajuste de inventario. Cada línea referencia un lote (idInventoryLot) o un producto (idProduct), nunca ambos. " +
                         "quantityDelta positivo = entrada, negativo = salida, cero = ajuste de costo puro. " +
                         "Ajuste por lote: unitCostNew requerido si quantityDelta > 0. " +
                         "Ajuste por producto (idProduct): quantityDelta siempre 0 y unitCostNew = costo promedio objetivo; ajusta todos los lotes del producto proporcionalmente.");

            // Exactamente uno de idInventoryLot o idProduct debe estar informado
            t.HasCheckConstraint("CK_inventoryAdjustmentLine_target",
                "(idInventoryLot IS NOT NULL AND idProduct IS NULL) OR (idInventoryLot IS NULL AND idProduct IS NOT NULL)");

            // Para líneas por lote con entrada: unitCostNew requerido
            t.HasCheckConstraint("CK_inventoryAdjustmentLine_unitCostNew",
                "idInventoryLot IS NULL OR quantityDelta <= 0 OR unitCostNew IS NOT NULL");

            // Para líneas por producto: siempre costo puro y unitCostNew requerido
            t.HasCheckConstraint("CK_inventoryAdjustmentLine_productLevel",
                "idProduct IS NULL OR (quantityDelta = 0 AND unitCostNew IS NOT NULL)");
        });

        builder.HasKey(ial => ial.IdInventoryAdjustmentLine);
        builder.Property(ial => ial.IdInventoryAdjustmentLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea.");

        builder.Property(ial => ial.IdInventoryAdjustment)
            .IsRequired()
            .HasComment("FK al ajuste de inventario cabecera. Cascade delete.");

        builder.Property(ial => ial.IdInventoryLot)
            .IsRequired(false)
            .HasComment("FK al lote de inventario a ajustar. Exclusivo con idProduct.");

        builder.Property(ial => ial.IdProduct)
            .IsRequired(false)
            .HasComment("FK al producto para ajuste de costo promedio global. Exclusivo con idInventoryLot. " +
                        "Al confirmar: escala el unitCost de todos sus lotes proporcionalmente para que el costo promedio ponderado = unitCostNew.");

        builder.Property(ial => ial.QuantityDelta)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro. Siempre 0 para líneas por producto.");

        builder.Property(ial => ial.UnitCostNew)
            .HasPrecision(18, 6)
            .HasComment("Costo unitario nuevo (ajuste por lote) o costo promedio objetivo (ajuste por producto). Requerido si quantityDelta > 0 o si se usa idProduct.");

        builder.Property(ial => ial.DescriptionLine)
            .HasMaxLength(500)
            .HasComment("Detalle por línea: insumo consumido, merma, motivo del ajuste, etc.");

        builder.HasIndex(ial => ial.IdInventoryAdjustment)
            .HasDatabaseName("IX_inventoryAdjustmentLine_idInventoryAdjustment");

        builder.HasIndex(ial => ial.IdProduct)
            .HasFilter("[idProduct] IS NOT NULL")
            .HasDatabaseName("IX_inventoryAdjustmentLine_idProduct");

        builder.HasOne(ial => ial.IdInventoryAdjustmentNavigation)
            .WithMany(ia => ia.InventoryAdjustmentLines)
            .HasForeignKey(ial => ial.IdInventoryAdjustment)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ial => ial.IdInventoryLotNavigation)
            .WithMany(il => il.AdjustmentLines)
            .HasForeignKey(ial => ial.IdInventoryLot)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ial => ial.IdProductNavigation)
            .WithMany()
            .HasForeignKey(ial => ial.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
