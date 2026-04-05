using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductOptionGroups.Dtos;

public sealed record CreateAvailabilityRuleRequest(
    [property: Required] int IdRestrictedItem,
    [property: Required] int IdEnablingItem);

public sealed record AvailabilityRuleResponse(
    int IdProductOptionItemAvailability,
    int IdRestrictedItem,
    int IdEnablingItem,
    string RestrictedItemName,
    string EnablingItemName);

public sealed record AvailableItemResponse(
    int    IdProductOptionItem,
    string NameItem,
    decimal PriceDelta,
    bool   IsDefault,
    int    SortOrder,
    int?   IdProductRecipe,
    bool   IsAvailable);

public sealed record AvailableGroupResponse(
    int    IdProductOptionGroup,
    string NameGroup,
    bool   IsRequired,
    int    MinSelections,
    int    MaxSelections,
    bool   AllowSplit,
    int    SortOrder,
    IReadOnlyList<AvailableItemResponse> Items);
