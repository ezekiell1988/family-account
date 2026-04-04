using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductComboSlots.Dtos;

public sealed record ProductComboSlotProductRequest(
    int     IdProduct,
    decimal PriceAdjustment,
    int     SortOrder);
