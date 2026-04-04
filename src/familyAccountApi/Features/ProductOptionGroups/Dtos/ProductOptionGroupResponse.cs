namespace FamilyAccountApi.Features.ProductOptionGroups.Dtos;

public sealed record ProductOptionGroupResponse(
    int                                   IdProductOptionGroup,
    int                                   IdProduct,
    string                                NameGroup,
    bool                                  IsRequired,
    int                                   MinSelections,
    int                                   MaxSelections,
    bool                                  AllowSplit,
    int                                   SortOrder,
    IReadOnlyList<ProductOptionItemResponse> Items);
