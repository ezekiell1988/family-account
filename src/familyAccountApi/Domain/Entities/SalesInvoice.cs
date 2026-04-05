namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesInvoice
{
    public int      IdSalesInvoice          { get; set; }
    public int      IdFiscalPeriod          { get; set; }
    public int      IdCurrency              { get; set; }
    public int      IdSalesInvoiceType      { get; set; }
    public int?     IdContact               { get; set; }   // Cliente (nullable para ventas al contado sin registro)
    public int?     IdBankAccount           { get; set; }   // Solo si CounterpartFromBankMovement = true
    public int?     IdSalesOrder            { get; set; }   // NULL = venta directa de tienda; NOT NULL = venta contra pedido
    public string   NumberInvoice           { get; set; } = null!;
    public DateOnly DateInvoice             { get; set; }
    public decimal  SubTotalAmount          { get; set; }
    public decimal  TaxAmount               { get; set; }
    public decimal  TotalAmount             { get; set; }
    public string   StatusInvoice           { get; set; } = null!;  // "Borrador" | "Confirmado" | "Anulado"
    public string?  DescriptionInvoice      { get; set; }
    public decimal  ExchangeRateValue       { get; set; }
    public DateTime CreatedAt               { get; set; }

    public FiscalPeriod      IdFiscalPeriodNavigation      { get; set; } = null!;
    public Currency          IdCurrencyNavigation          { get; set; } = null!;
    public SalesInvoiceType  IdSalesInvoiceTypeNavigation  { get; set; } = null!;
    public Contact?          IdContactNavigation           { get; set; }
    public BankAccount?      IdBankAccountNavigation       { get; set; }
    public SalesOrder?       IdSalesOrderNavigation        { get; set; }

    public ICollection<SalesInvoiceLine>  SalesInvoiceLines   { get; set; } = [];
    public ICollection<SalesInvoiceEntry> SalesInvoiceEntries { get; set; } = [];
    public ICollection<BankMovementDocument> BankMovementDocuments { get; set; } = [];
}
