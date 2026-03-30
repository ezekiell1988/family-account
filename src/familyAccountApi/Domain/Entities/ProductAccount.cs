namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductAccount
{
    public int     IdProductAccount    { get; set; }
    public int     IdProduct           { get; set; }
    public int     IdAccount           { get; set; }
    public int?    IdCostCenter        { get; set; }
    public decimal PercentageAccount   { get; set; }

    public Product    IdProductNavigation   { get; set; } = null!;
    public Account    IdAccountNavigation   { get; set; } = null!;
    public CostCenter? IdCostCenterNavigation { get; set; }
}
