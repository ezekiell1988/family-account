namespace FamilyAccountApi.Features.ProductionOrders.Dtos;

public sealed record ProductionOrderResponse(
    int       IdProductionOrder,
    int       IdFiscalPeriod,
    int?      IdSalesOrder,
    string?   SalesOrderNumber,
    string?   ContactName,
    string    NumberProductionOrder,
    DateOnly  DateOrder,
    DateOnly? DateRequired,
    string    StatusProductionOrder,
    string?   DescriptionOrder,
    int?      IdWarehouse,
    string?   WarehouseName,
    DateTime  CreatedAt,
    IReadOnlyList<ProductionOrderLineResponse> Lines);

public sealed record ProductionOrderLineResponse(
    int      IdProductionOrderLine,
    int      IdProduct,
    string   ProductName,
    int      IdProductUnit,
    string   UnitName,
    int?     IdSalesOrderLine,
    decimal  QuantityRequired,
    decimal  QuantityProduced,
    decimal  QuantityPending,
    string?  DescriptionLine);
