namespace FamilyAccountApi.Features.ProductOptionGroups.Dtos;

public sealed record ProductOptionItemResponse(
    int     IdProductOptionItem,
    string  NameItem,
    decimal PriceDelta,
    bool    IsDefault,
    int     SortOrder,
    int?    IdProductRecipe);
