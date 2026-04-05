namespace FamilyAccountApi.Domain.Entities;

/// <summary>
/// Tabla pivot N:M entre inventoryAdjustment y accountingEntry.
/// Un ajuste puede vincularse a más de un asiento en el futuro (confirmación + reversión).
/// </summary>
public sealed class InventoryAdjustmentEntry
{
    public int IdInventoryAdjustmentEntry { get; set; }
    public int IdInventoryAdjustment      { get; set; }
    public int IdAccountingEntry          { get; set; }

    public InventoryAdjustment IdInventoryAdjustmentNavigation { get; set; } = null!;
    public AccountingEntry     IdAccountingEntryNavigation     { get; set; } = null!;
}
