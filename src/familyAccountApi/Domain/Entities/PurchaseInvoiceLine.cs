namespace FamilyAccountApi.Domain.Entities;

public sealed class PurchaseInvoiceLine
{
    public int     IdPurchaseInvoiceLine { get; set; }
    public int     IdPurchaseInvoice     { get; set; }
    public int?    IdProductSKU          { get; set; }
    public string  DescriptionLine       { get; set; } = null!;
    public decimal Quantity              { get; set; }
    public decimal UnitPrice             { get; set; }
    public decimal TaxPercent            { get; set; }
    public decimal TotalLineAmount       { get; set; }

    public PurchaseInvoice  IdPurchaseInvoiceNavigation { get; set; } = null!;
    public ProductSKU?      IdProductSKUNavigation      { get; set; }
    public ICollection<PurchaseInvoiceLineEntry> PurchaseInvoiceLineEntries { get; set; } = [];
}
