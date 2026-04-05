namespace FamilyAccountApi.Features.SalesOrders.Dtos;

public sealed record SalesOrderResponse(
    int       IdSalesOrder,
    int       IdFiscalPeriod,
    int       IdCurrency,
    string    CurrencyCode,
    int       IdContact,
    string    ContactName,
    int?      IdPriceList,
    string?   PriceListName,
    string    NumberOrder,
    DateOnly  DateOrder,
    DateOnly? DateDelivery,
    decimal   SubTotalAmount,
    decimal   TaxAmount,
    decimal   TotalAmount,
    decimal   ExchangeRateValue,
    string    StatusOrder,
    string?   DescriptionOrder,
    DateTime  CreatedAt,
    IReadOnlyList<SalesOrderLineResponse> Lines);

public sealed record SalesOrderLineResponse(
    int      IdSalesOrderLine,
    int      IdProduct,
    string   ProductName,
    int      IdProductUnit,
    string   UnitName,
    int?     IdPriceListItem,
    decimal  Quantity,
    decimal  QuantityBase,
    decimal  UnitPrice,
    decimal  TaxPercent,
    decimal  TotalLineAmount,
    string?  DescriptionLine);
