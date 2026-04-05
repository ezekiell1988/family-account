namespace FamilyAccountApi.Features.Warehouses.Dtos;

public sealed record WarehouseResponse(
    int    IdWarehouse,
    string NameWarehouse,
    bool   IsDefault,
    bool   IsActive);
