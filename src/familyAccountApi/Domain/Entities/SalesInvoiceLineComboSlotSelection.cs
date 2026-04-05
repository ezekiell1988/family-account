namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesInvoiceLineComboSlotSelection
{
    public int  IdSalesInvoiceLineComboSlotSelection { get; set; }
    public int  IdSalesInvoiceLine                   { get; set; }
    public int  IdProductComboSlot                   { get; set; }
    public int  IdProduct                            { get; set; }   // Producto elegido en el slot (snapshot)
    public int? IdInventoryLot                       { get; set; }   // Lote PT pre-asignado desde producción (nullable)

    public SalesInvoiceLine  IdSalesInvoiceLineNavigation  { get; set; } = null!;
    public ProductComboSlot  IdProductComboSlotNavigation  { get; set; } = null!;
    public Product           IdProductNavigation           { get; set; } = null!;
    public InventoryLot?     IdInventoryLotNavigation      { get; set; }

    public ICollection<SalesInvoiceLineSlotOption> SalesInvoiceLineSlotOptions { get; set; } = [];
}
