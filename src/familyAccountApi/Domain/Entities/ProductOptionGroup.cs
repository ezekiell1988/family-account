namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductOptionGroup
{
    public int    IdProductOptionGroup { get; set; }
    public int    IdProduct            { get; set; }
    public string NameGroup            { get; set; } = null!;
    public bool   IsRequired           { get; set; }
    public int    MinSelections        { get; set; }
    public int    MaxSelections        { get; set; }
    public bool   AllowSplit           { get; set; } = false;
    public int    SortOrder            { get; set; }

    public Product IdProductNavigation { get; set; } = null!;

    public ICollection<ProductOptionItem> ProductOptionItems { get; set; } = [];
}
