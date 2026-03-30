namespace FamilyAccountApi.Domain.Entities;

public sealed class PurchaseInvoice
{
    public int      IdPurchaseInvoice       { get; set; }
    public int      IdFiscalPeriod          { get; set; }
    public int      IdCurrency              { get; set; }
    public int      IdPurchaseInvoiceType   { get; set; }
    public int?     IdBankAccount           { get; set; }
    public string   NumberInvoice           { get; set; } = null!;
    public string   ProviderName            { get; set; } = null!;
    public DateOnly DateInvoice             { get; set; }
    public decimal  SubTotalAmount          { get; set; }
    public decimal  TaxAmount               { get; set; }
    public decimal  TotalAmount             { get; set; }
    public string   StatusInvoice           { get; set; } = null!;  // "Borrador" | "Confirmado" | "Anulado"
    public string?  DescriptionInvoice      { get; set; }
    public decimal  ExchangeRateValue       { get; set; }
    public DateTime CreatedAt               { get; set; }

    public FiscalPeriod         IdFiscalPeriodNavigation        { get; set; } = null!;
    public Currency             IdCurrencyNavigation            { get; set; } = null!;
    public PurchaseInvoiceType  IdPurchaseInvoiceTypeNavigation { get; set; } = null!;
    public BankAccount?         IdBankAccountNavigation         { get; set; }
    public ICollection<PurchaseInvoiceLine>  PurchaseInvoiceLines  { get; set; } = [];
    public ICollection<PurchaseInvoiceEntry> PurchaseInvoiceEntries { get; set; } = [];
    public ICollection<BankMovementDocument> BankMovementDocuments { get; set; } = [];
}
