namespace FamilyAccountApi.Features.InventoryAdjustmentTypes.Dtos;

public sealed record InventoryAdjustmentTypeResponse(
    int     IdInventoryAdjustmentType,
    string  CodeInventoryAdjustmentType,
    string  NameInventoryAdjustmentType,
    int?    IdAccountInventoryDefault,
    string? CodeAccountInventoryDefault,
    string? NameAccountInventoryDefault,
    int?    IdAccountCounterpartEntry,
    string? CodeAccountCounterpartEntry,
    string? NameAccountCounterpartEntry,
    int?    IdAccountCounterpartExit,
    string? CodeAccountCounterpartExit,
    string? NameAccountCounterpartExit,
    bool    IsActive);
