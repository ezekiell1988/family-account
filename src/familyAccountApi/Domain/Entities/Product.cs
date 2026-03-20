namespace FamilyAccountApi.Domain.Entities;

public sealed class Product
{
    public int    IdProduct   { get; set; }
    public string CodeProduct { get; set; } = null!;
    public string NameProduct { get; set; } = null!;

    public ICollection<ProductProductSKU>      ProductProductSKUs      { get; set; } = [];
    public ICollection<ProductProductCategory> ProductProductCategories { get; set; } = [];
}
