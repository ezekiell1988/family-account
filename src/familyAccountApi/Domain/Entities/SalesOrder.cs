namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesOrder
{
    public int      IdSalesOrder       { get; set; }
    public int      IdFiscalPeriod     { get; set; }
    public int      IdCurrency         { get; set; }
    public int      IdContact          { get; set; }   // Cliente
    public int?     IdPriceList        { get; set; }   // Lista de precios vigente al crear el pedido (snapshot)
    public string   NumberOrder        { get; set; } = null!;
    public DateOnly DateOrder          { get; set; }
    public DateOnly? DateDelivery      { get; set; }   // Fecha compromiso de entrega
    public decimal  SubTotalAmount     { get; set; }
    public decimal  TaxAmount          { get; set; }
    public decimal  TotalAmount        { get; set; }
    public decimal  ExchangeRateValue  { get; set; }
    public string   StatusOrder        { get; set; } = null!;  // "Borrador"|"Confirmado"|"EnProduccion"|"Completado"|"Anulado"
    public string?  DescriptionOrder   { get; set; }
    public DateTime CreatedAt          { get; set; }

    public FiscalPeriod  IdFiscalPeriodNavigation { get; set; } = null!;
    public Currency      IdCurrencyNavigation     { get; set; } = null!;
    public Contact       IdContactNavigation      { get; set; } = null!;
    public PriceList?    IdPriceListNavigation    { get; set; }

    public ICollection<SalesOrderLine>    SalesOrderLines    { get; set; } = [];
    public ICollection<SalesOrderAdvance> SalesOrderAdvances { get; set; } = [];
    public ICollection<SalesInvoice>      SalesInvoices      { get; set; } = [];
    public ICollection<ProductionOrder>   ProductionOrders   { get; set; } = [];
}
