namespace FamilyAccountApi.Domain.Entities;

public sealed class InventoryAdjustment
{
    public int      IdInventoryAdjustment    { get; set; }
    public int      IdFiscalPeriod           { get; set; }
    public string   TypeAdjustment           { get; set; } = null!;
    public string   NumberAdjustment         { get; set; } = null!;
    public DateOnly DateAdjustment           { get; set; }
    public string?  DescriptionAdjustment    { get; set; }
    public string   StatusAdjustment         { get; set; } = null!;
    public DateTime CreatedAt                { get; set; }

    public FiscalPeriod IdFiscalPeriodNavigation { get; set; } = null!;
    public ICollection<InventoryAdjustmentLine> InventoryAdjustmentLines { get; set; } = [];
}
