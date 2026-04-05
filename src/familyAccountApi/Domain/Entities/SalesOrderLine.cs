namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesOrderLine
{
    public int      IdSalesOrderLine   { get; set; }
    public int      IdSalesOrder       { get; set; }
    public int      IdProduct          { get; set; }
    public int      IdProductUnit      { get; set; }   // Unidad solicitada
    public int?     IdPriceListItem    { get; set; }   // Snapshot del ítem de lista usado para el precio
    public decimal  Quantity           { get; set; }
    public decimal  QuantityBase       { get; set; }   // Calculada × ConversionFactor al confirmar
    public decimal  UnitPrice          { get; set; }   // Snapshot del precio al crear el pedido
    public decimal  TaxPercent         { get; set; }
    public decimal  TotalLineAmount    { get; set; }
    public string?  DescriptionLine    { get; set; }

    public SalesOrder     IdSalesOrderNavigation     { get; set; } = null!;
    public Product        IdProductNavigation        { get; set; } = null!;
    public ProductUnit    IdProductUnitNavigation    { get; set; } = null!;
    public PriceListItem? IdPriceListItemNavigation  { get; set; }

    public ICollection<SalesOrderLineFulfillment>          SalesOrderLineFulfillments { get; set; } = [];
    public ICollection<SalesOrderLineOption>                 SalesOrderLineOptions      { get; set; } = [];
    public ICollection<SalesOrderLineComboSlotSelection>     ComboSlotSelections        { get; set; } = [];
}
