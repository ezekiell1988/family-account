namespace FamilyAccountApi.Features.InventoryAdjustments.Dtos;

public sealed record InventoryAdjustmentLineResponse(
    int      IdInventoryAdjustmentLine,
    int?     IdInventoryLot,
    int?     IdProduct,
    string   NameProduct,
    string?  LotNumber,
    decimal  QuantityDelta,
    decimal? UnitCostNew,
    string?  DescriptionLine);

public sealed record InventoryAdjustmentResponse(
    int      IdInventoryAdjustment,
    int      IdFiscalPeriod,
    int      IdInventoryAdjustmentType,
    string   CodeInventoryAdjustmentType,
    string   NameInventoryAdjustmentType,
    int      IdCurrency,
    string   CodeCurrency,
    decimal  ExchangeRateValue,
    string   NumberAdjustment,
    DateOnly DateAdjustment,
    string?  DescriptionAdjustment,
    string   StatusAdjustment,
    DateTime CreatedAt,
    int?     IdAccountingEntry,
    IReadOnlyList<InventoryAdjustmentLineResponse> Lines);
