namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductProductSKU
{
    public int IdProductProductSKU { get; set; }
    public int IdProduct           { get; set; }
    public int IdProductSKU        { get; set; }

    public Product    Product    { get; set; } = null!;
    public ProductSKU ProductSKU { get; set; } = null!;
}
