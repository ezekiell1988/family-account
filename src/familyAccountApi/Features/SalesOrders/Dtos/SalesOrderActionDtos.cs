namespace FamilyAccountApi.Features.SalesOrders.Dtos;

public sealed record SendToProductionResponse(
    IReadOnlyList<CreatedProductionOrderInfo> ProductionOrders);

public sealed record CreatedProductionOrderInfo(
    int    IdProductionOrder,
    string NumberProductionOrder,
    int    IdSalesOrderLine,
    string ProductName);

public sealed record GenerateInvoiceResponse(int IdSalesInvoice);
