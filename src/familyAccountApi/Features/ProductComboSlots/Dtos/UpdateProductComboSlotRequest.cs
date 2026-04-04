using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductComboSlots.Dtos;

public sealed record UpdateProductComboSlotRequest(
    [Required, MaxLength(200)] string           NameSlot,
    decimal                                     Quantity,
    bool                                        IsRequired,
    int                                         SortOrder,
    [Required, MinLength(1)] IReadOnlyList<ProductComboSlotProductRequest> Products);
