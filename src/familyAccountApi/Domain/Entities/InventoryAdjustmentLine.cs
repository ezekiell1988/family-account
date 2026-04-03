namespace FamilyAccountApi.Domain.Entities;

public sealed class InventoryAdjustmentLine
{
    public int      IdInventoryAdjustmentLine { get; set; }
    public int      IdInventoryAdjustment     { get; set; }
    public int      IdInventoryLot            { get; set; }
    public decimal  QuantityDelta             { get; set; }
    public decimal? UnitCostNew               { get; set; }
    public string?  DescriptionLine           { get; set; }

    public InventoryAdjustment IdInventoryAdjustmentNavigation { get; set; } = null!;
    public InventoryLot        IdInventoryLotNavigation        { get; set; } = null!;
}
