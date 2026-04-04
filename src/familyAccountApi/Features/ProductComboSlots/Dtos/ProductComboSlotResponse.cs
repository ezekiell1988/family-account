namespace FamilyAccountApi.Features.ProductComboSlots.Dtos;

public sealed record ProductComboSlotResponse(
    int                                        IdProductComboSlot,
    int                                        IdProductCombo,
    string                                     NameSlot,
    decimal                                    Quantity,
    bool                                       IsRequired,
    int                                        SortOrder,
    IReadOnlyList<ProductComboSlotProductResponse> Products);
