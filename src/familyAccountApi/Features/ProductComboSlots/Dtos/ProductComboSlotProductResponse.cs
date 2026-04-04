namespace FamilyAccountApi.Features.ProductComboSlots.Dtos;

public sealed record ProductComboSlotProductResponse(
    int     IdProductComboSlotProduct,
    int     IdProduct,
    string  NameProduct,
    decimal PriceAdjustment,
    int     SortOrder);
