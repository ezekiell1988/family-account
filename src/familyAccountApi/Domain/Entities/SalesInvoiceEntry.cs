namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesInvoiceEntry
{
    public int IdSalesInvoiceEntry  { get; set; }
    public int IdSalesInvoice       { get; set; }
    public int IdAccountingEntry    { get; set; }

    public SalesInvoice    IdSalesInvoiceNavigation    { get; set; } = null!;
    public AccountingEntry IdAccountingEntryNavigation { get; set; } = null!;
}
