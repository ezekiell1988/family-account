namespace FamilyAccountApi.Features.InventoryLots.Dtos;

public sealed record InventoryLotResponse(
    int      IdInventoryLot,
    int      IdProduct,
    string   NameProduct,
    string?  LotNumber,
    DateOnly? ExpirationDate,
    decimal  UnitCost,
    decimal  QuantityAvailable,
    decimal  QuantityReserved,
    decimal  QuantityAvailableNet,
    string   CodeUnit,
    string   StatusLot,
    string   SourceType,
    int?     IdPurchaseInvoice,
    int?     IdInventoryAdjustment,
    DateTime CreatedAt,
    int      IdWarehouse,
    string   NameWarehouse);
