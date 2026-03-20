namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductSKU
{
    public int     IdProductSKU          { get; set; }
    public string  CodeProductSKU        { get; set; } = null!;
    public string  NameProductSKU        { get; set; } = null!;
    public string? BrandProductSKU       { get; set; }
    public string? DescriptionProductSKU { get; set; }
    public string? NetContent            { get; set; }

    public ICollection<ProductProductSKU> ProductProductSKUs { get; set; } = [];
}
