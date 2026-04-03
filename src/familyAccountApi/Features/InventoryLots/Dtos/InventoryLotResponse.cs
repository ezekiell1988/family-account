namespace FamilyAccountApi.Features.InventoryLots.Dtos;

public sealed record InventoryLotResponse(
    int      IdInventoryLot,
    int      IdProduct,
    string   NameProduct,
    string?  LotNumber,
    DateOnly? ExpirationDate,
    decimal  UnitCost,
    decimal  QuantityAvailable,
    string   CodeUnit,
    string   SourceType,
    int?     IdPurchaseInvoice,
    int?     IdInventoryAdjustment,
    DateTime CreatedAt);
