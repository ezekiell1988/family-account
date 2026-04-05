namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesInvoiceLine
{
    public int      IdSalesInvoiceLine  { get; set; }
    public int      IdSalesInvoice      { get; set; }
    public bool     IsNonProductLine    { get; set; }   // true = flete/servicio/gasto; false = producto con stock
    public int?     IdProduct           { get; set; }
    public int?     IdUnit              { get; set; }
    public int?     IdInventoryLot      { get; set; }   // Requerido cuando IsNonProductLine = false Y no es BOM/combo (RESTRICT)
    public int?     IdProductRecipe     { get; set; }   // Snapshot de la receta usada al confirmar por explosión BOM (nullable)
    public string   DescriptionLine     { get; set; } = null!;
    public decimal  Quantity            { get; set; }
    public decimal? QuantityBase        { get; set; }   // Calculada al confirmar × ConversionFactor
    public decimal  UnitPrice           { get; set; }
    public decimal? UnitCost            { get; set; }   // Snapshot de AverageCost al confirmar
    public decimal  TaxPercent          { get; set; }
    public decimal  TotalLineAmount     { get; set; }

    public SalesInvoice    IdSalesInvoiceNavigation    { get; set; } = null!;
    public Product?        IdProductNavigation         { get; set; }
    public UnitOfMeasure?  IdUnitNavigation            { get; set; }
    public InventoryLot?   IdInventoryLotNavigation    { get; set; }
    public ProductRecipe?  IdProductRecipeNavigation   { get; set; }

    public ICollection<SalesInvoiceLineEntry>    SalesInvoiceLineEntries { get; set; } = [];
    public ICollection<SalesInvoiceLineBomDetail> BomDetails             { get; set; } = [];
}
