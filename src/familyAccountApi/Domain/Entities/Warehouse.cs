namespace FamilyAccountApi.Domain.Entities;

public sealed class Warehouse
{
    public int    IdWarehouse   { get; set; }
    public string NameWarehouse { get; set; } = null!;
    public bool   IsDefault     { get; set; }
    public bool   IsActive      { get; set; } = true;

    public ICollection<InventoryLot>   InventoryLots   { get; set; } = [];
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = [];
}
