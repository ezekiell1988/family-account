namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesInvoiceLineEntry
{
    public int IdSalesInvoiceLineEntry  { get; set; }
    public int IdSalesInvoiceLine       { get; set; }
    public int IdAccountingEntryLine    { get; set; }

    public SalesInvoiceLine    IdSalesInvoiceLineNavigation    { get; set; } = null!;
    public AccountingEntryLine IdAccountingEntryLineNavigation { get; set; } = null!;
}
