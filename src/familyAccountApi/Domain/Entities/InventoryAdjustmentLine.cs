namespace FamilyAccountApi.Domain.Entities;

public sealed class InventoryAdjustmentLine
{
    public int      IdInventoryAdjustmentLine { get; set; }
    public int      IdInventoryAdjustment     { get; set; }
    /// <summary>FK al lote específico. Exclusivo con IdProduct.</summary>
    public int?     IdInventoryLot            { get; set; }
    /// <summary>FK al producto para ajuste de costo promedio global. Exclusivo con IdInventoryLot.</summary>
    public int?     IdProduct                 { get; set; }
    public decimal  QuantityDelta             { get; set; }
    public decimal? UnitCostNew               { get; set; }
    public string?  DescriptionLine           { get; set; }

    public InventoryAdjustment IdInventoryAdjustmentNavigation { get; set; } = null!;
    public InventoryLot?       IdInventoryLotNavigation        { get; set; }
    public Product?            IdProductNavigation             { get; set; }
}
