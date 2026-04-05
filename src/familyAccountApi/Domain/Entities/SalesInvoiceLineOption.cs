namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesInvoiceLineOption
{
    public int     IdSalesInvoiceLineOption { get; set; }
    public int     IdSalesInvoiceLine       { get; set; }
    public int     IdProductOptionItem      { get; set; }
    public decimal Quantity                 { get; set; } = 1m;

    public SalesInvoiceLine  IdSalesInvoiceLineNavigation  { get; set; } = null!;
    public ProductOptionItem IdProductOptionItemNavigation { get; set; } = null!;
}
