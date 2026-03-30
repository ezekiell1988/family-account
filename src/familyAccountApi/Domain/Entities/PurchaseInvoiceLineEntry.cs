namespace FamilyAccountApi.Domain.Entities;

public sealed class PurchaseInvoiceLineEntry
{
    public int IdPurchaseInvoiceLineEntry { get; set; }
    public int IdPurchaseInvoiceLine      { get; set; }
    public int IdAccountingEntryLine      { get; set; }

    public PurchaseInvoiceLine  IdPurchaseInvoiceLineNavigation  { get; set; } = null!;
    public AccountingEntryLine  IdAccountingEntryLineNavigation  { get; set; } = null!;
}
