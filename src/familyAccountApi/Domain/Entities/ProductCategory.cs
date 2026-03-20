namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductCategory
{
    public int    IdProductCategory   { get; set; }
    public string NameProductCategory { get; set; } = null!;

    public ICollection<ProductProductCategory> ProductProductCategories { get; set; } = [];
}
