namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductionOrder
{
    public int      IdProductionOrder    { get; set; }
    public int      IdFiscalPeriod       { get; set; }
    public int?     IdSalesOrder         { get; set; }   // NULL = Modalidad A (producción para stock)
    public string   NumberProductionOrder { get; set; } = null!;
    public DateOnly DateOrder             { get; set; }
    public DateOnly? DateRequired         { get; set; }   // Fecha requerida de entrega
    public string   StatusProductionOrder { get; set; } = null!;  // "Borrador"|"Pendiente"|"EnProceso"|"Completado"|"Anulado"
    public string?  DescriptionOrder      { get; set; }
    public DateTime CreatedAt             { get; set; }

    public FiscalPeriod  IdFiscalPeriodNavigation { get; set; } = null!;
    public SalesOrder?   IdSalesOrderNavigation   { get; set; }

    public ICollection<ProductionOrderLine>          ProductionOrderLines          { get; set; } = [];
    public ICollection<InventoryAdjustment>          InventoryAdjustments          { get; set; } = [];
    public ICollection<SalesOrderLineFulfillment>    SalesOrderLineFulfillments    { get; set; } = [];
    public ICollection<SalesOrderAdvance>            SalesOrderAdvances            { get; set; } = [];
}
