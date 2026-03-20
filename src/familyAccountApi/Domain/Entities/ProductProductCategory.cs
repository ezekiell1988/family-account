namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductProductCategory
{
    public int IdProductProductCategory { get; set; }
    public int IdProduct                { get; set; }
    public int IdProductCategory        { get; set; }

    public Product         Product         { get; set; } = null!;
    public ProductCategory ProductCategory { get; set; } = null!;
}
