namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductRecipeLine
{
    public int     IdProductRecipeLine { get; set; }
    public int     IdProductRecipe     { get; set; }
    public int     IdProductInput      { get; set; }
    public decimal QuantityInput       { get; set; }
    public int     SortOrder           { get; set; }

    public ProductRecipe IdProductRecipeNavigation  { get; set; } = null!;
    public Product       IdProductInputNavigation   { get; set; } = null!;
}
