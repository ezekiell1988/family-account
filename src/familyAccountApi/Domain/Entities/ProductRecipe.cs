namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductRecipe
{
    public int       IdProductRecipe     { get; set; }
    public int       IdProductOutput     { get; set; }
    public string    NameRecipe          { get; set; } = null!;
    public decimal   QuantityOutput      { get; set; }
    public string?   DescriptionRecipe   { get; set; }
    public bool      IsActive            { get; set; }
    public DateTime  CreatedAt           { get; set; }

    public Product IdProductOutputNavigation { get; set; } = null!;
    public ICollection<ProductRecipeLine> ProductRecipeLines { get; set; } = [];
}
