namespace FamilyAccountApi.Domain.Entities;

public sealed class AccountingEntry
{
    public int      IdAccountingEntry { get; set; }
    public int      IdFiscalPeriod    { get; set; }
    public int      IdCurrency        { get; set; }
    public string   NumberEntry       { get; set; } = null!;
    public DateOnly DateEntry         { get; set; }
    public string   DescriptionEntry  { get; set; } = null!;
    public string   StatusEntry       { get; set; } = null!;
    public string?  ReferenceEntry    { get; set; }
    public decimal  ExchangeRateValue { get; set; }
    public DateTime CreatedAt         { get; set; }
    public string?  OriginModule      { get; set; }  // null | "BankMovement" | "PurchaseInvoice"
    public int?     IdOriginRecord    { get; set; }  // ID del registro origen

    public FiscalPeriod IdFiscalPeriodNavigation { get; set; } = null!;
    public Currency IdCurrencyNavigation { get; set; } = null!;
    public ICollection<AccountingEntryLine> AccountingEntryLines { get; set; } = [];
}
