namespace FamilyAccountApi.Features.ProductRecipes.Dtos;

public sealed record ProductRecipeLineResponse(
    int     IdProductRecipeLine,
    int     IdProductInput,
    string  NameProductInput,
    decimal QuantityInput,
    string  CodeUnitInput,
    int     SortOrder);

public sealed record ProductRecipeResponse(
    int      IdProductRecipe,
    int      IdProductOutput,
    string   NameProductOutput,
    int      VersionNumber,
    string   NameRecipe,
    decimal  QuantityOutput,
    string   CodeUnitOutput,
    string?  DescriptionRecipe,
    bool     IsActive,
    DateTime CreatedAt,
    IReadOnlyList<ProductRecipeLineResponse> Lines);
