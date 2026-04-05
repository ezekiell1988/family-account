namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductType
{
    public int     IdProductType          { get; set; }
    public string  NameProductType        { get; set; } = null!;
    public string? DescriptionProductType { get; set; }
    public bool    TrackInventory         { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
