using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductOptionGroups.Dtos;

public sealed record ProductOptionItemRequest(
    [Required, MaxLength(200)] string  NameItem,
    decimal                            PriceDelta,
    bool                               IsDefault,
    int                                SortOrder);
