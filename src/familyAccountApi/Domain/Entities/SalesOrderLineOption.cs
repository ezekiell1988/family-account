namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesOrderLineOption
{
    public int     IdSalesOrderLineOption { get; set; }
    public int     IdSalesOrderLine       { get; set; }
    public int     IdProductOptionItem    { get; set; }
    public decimal Quantity               { get; set; } = 1m;

    public SalesOrderLine    IdSalesOrderLineNavigation    { get; set; } = null!;
    public ProductOptionItem IdProductOptionItemNavigation { get; set; } = null!;
}
