namespace FamilyAccountApi.Domain.Entities;

public sealed class PurchaseInvoiceEntry
{
    public int IdPurchaseInvoiceEntry { get; set; }
    public int IdPurchaseInvoice      { get; set; }
    public int IdAccountingEntry      { get; set; }

    public PurchaseInvoice  IdPurchaseInvoiceNavigation  { get; set; } = null!;
    public AccountingEntry  IdAccountingEntryNavigation  { get; set; } = null!;
}
