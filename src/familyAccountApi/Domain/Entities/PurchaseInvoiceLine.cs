namespace FamilyAccountApi.Domain.Entities;

public sealed class PurchaseInvoiceLine
{
    public int      IdPurchaseInvoiceLine { get; set; }
    public int      IdPurchaseInvoice     { get; set; }
    public int?     IdProduct             { get; set; }
    public int?     IdUnit                { get; set; }
    public string   DescriptionLine       { get; set; } = null!;
    public decimal  Quantity              { get; set; }
    public decimal? QuantityBase          { get; set; }
    public decimal  UnitPrice             { get; set; }
    public decimal  TaxPercent            { get; set; }
    public decimal  TotalLineAmount       { get; set; }
    public string?  LotNumber             { get; set; }
    public DateOnly? ExpirationDate       { get; set; }

    public PurchaseInvoice  IdPurchaseInvoiceNavigation { get; set; } = null!;
    public Product?         IdProductNavigation         { get; set; }
    public UnitOfMeasure?   IdUnitNavigation            { get; set; }
    public ICollection<PurchaseInvoiceLineEntry> PurchaseInvoiceLineEntries { get; set; } = [];
}
