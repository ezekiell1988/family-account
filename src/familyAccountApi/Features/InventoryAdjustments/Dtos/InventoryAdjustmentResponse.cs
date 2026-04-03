namespace FamilyAccountApi.Features.InventoryAdjustments.Dtos;

public sealed record InventoryAdjustmentLineResponse(
    int      IdInventoryAdjustmentLine,
    int      IdInventoryLot,
    string   NameProduct,
    string?  LotNumber,
    decimal  QuantityDelta,
    decimal? UnitCostNew,
    string?  DescriptionLine);

public sealed record InventoryAdjustmentResponse(
    int      IdInventoryAdjustment,
    int      IdFiscalPeriod,
    string   TypeAdjustment,
    string   NumberAdjustment,
    DateOnly DateAdjustment,
    string?  DescriptionAdjustment,
    string   StatusAdjustment,
    DateTime CreatedAt,
    IReadOnlyList<InventoryAdjustmentLineResponse> Lines);
