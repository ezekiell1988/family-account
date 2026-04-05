namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductVariantAttribute
{
    public int IdProductVariantAttribute { get; set; }
    public int IdProduct                 { get; set; }
    public int IdAttributeValue          { get; set; }

    public Product        IdProductNavigation        { get; set; } = null!;
    public AttributeValue IdAttributeValueNavigation { get; set; } = null!;
}
