using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductOptionGroups.Dtos;

public sealed record UpdateProductOptionGroupRequest(
    [Required, MaxLength(200)] string       NameGroup,
    bool                                    IsRequired,
    int                                     MinSelections,
    int                                     MaxSelections,
    bool                                    AllowSplit,
    int                                     SortOrder,
    [Required, MinLength(1)] IReadOnlyList<ProductOptionItemRequest> Items);
