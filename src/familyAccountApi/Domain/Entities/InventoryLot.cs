namespace FamilyAccountApi.Domain.Entities;

public sealed class InventoryLot
{
    public int       IdInventoryLot          { get; set; }
    public int       IdProduct               { get; set; }
    public string?   LotNumber               { get; set; }
    public DateOnly? ExpirationDate          { get; set; }
    public decimal   UnitCost                { get; set; }
    public decimal   QuantityAvailable       { get; set; }
    public string    SourceType              { get; set; } = null!;
    public int?      IdPurchaseInvoice       { get; set; }
    public int?      IdInventoryAdjustment   { get; set; }
    public DateTime  CreatedAt               { get; set; }

    public Product            Product                           { get; set; } = null!;
    public PurchaseInvoice?   IdPurchaseInvoiceNavigation       { get; set; }
    public InventoryAdjustment? IdInventoryAdjustmentNavigation { get; set; }

    public ICollection<InventoryAdjustmentLine> AdjustmentLines { get; set; } = [];
}
