namespace FamilyAccountApi.Features.ProductComboSlots.Dtos;

public sealed record ProductComboSlotPresetOptionResponse(
    int    IdProductComboSlotPresetOption,
    int    IdProductOptionItem,
    string NameItem,
    decimal PriceDelta);
