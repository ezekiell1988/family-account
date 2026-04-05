namespace FamilyAccountApi.Domain.Entities;

public sealed class AttributeValue
{
    public int    IdAttributeValue    { get; set; }
    public int    IdProductAttribute  { get; set; }
    public string NameValue           { get; set; } = null!;
    public int    SortOrder           { get; set; }

    public ProductAttribute                     IdProductAttributeNavigation { get; set; } = null!;
    public ICollection<ProductVariantAttribute> ProductVariantAttributes    { get; set; } = [];
}
