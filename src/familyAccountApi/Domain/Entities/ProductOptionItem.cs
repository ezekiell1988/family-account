namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductOptionItem
{
    public int     IdProductOptionItem  { get; set; }
    public int     IdProductOptionGroup { get; set; }
    public string  NameItem             { get; set; } = null!;
    public decimal PriceDelta           { get; set; }
    public bool    IsDefault            { get; set; } = false;
    public int     SortOrder            { get; set; }
    public int?    IdProductRecipe      { get; set; }

    public ProductOptionGroup  IdProductOptionGroupNavigation { get; set; } = null!;
    public ProductRecipe?      IdProductRecipeNavigation      { get; set; }

    public ICollection<ProductOptionItemAvailability> RestrictedByRules  { get; set; } = [];
    public ICollection<ProductOptionItemAvailability> EnablesRules        { get; set; } = [];
}
