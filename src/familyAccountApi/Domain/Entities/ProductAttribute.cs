namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductAttribute
{
    public int    IdProductAttribute { get; set; }
    public int    IdProduct          { get; set; }
    public string NameAttribute      { get; set; } = null!;
    public int    SortOrder          { get; set; }

    public Product                    IdProductNavigation  { get; set; } = null!;
    public ICollection<AttributeValue> AttributeValues     { get; set; } = [];
}
