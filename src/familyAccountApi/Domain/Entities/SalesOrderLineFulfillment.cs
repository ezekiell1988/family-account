namespace FamilyAccountApi.Domain.Entities;

/// <summary>
/// Detalle de cómo se cumple cada línea del pedido:
/// puede usarse stock existente ("Stock") o producción ("Produccion"), o ambos.
/// </summary>
public sealed class SalesOrderLineFulfillment
{
    public int      IdSalesOrderLineFulfillment { get; set; }
    public int      IdSalesOrderLine            { get; set; }
    public string   FulfillmentType             { get; set; } = null!;  // "Stock" | "Produccion"
    public int?     IdInventoryLot              { get; set; }   // FK cuando FulfillmentType = "Stock"
    public int?     IdProductionOrder           { get; set; }   // FK cuando FulfillmentType = "Produccion"
    public decimal  QuantityBase                { get; set; }   // Cantidad asignada en unidad base
    public decimal? UnitCost                    { get; set; }   // Snapshot de costo al confirmar
    public DateTime CreatedAt                   { get; set; }

    public SalesOrderLine    IdSalesOrderLineNavigation    { get; set; } = null!;
    public InventoryLot?     IdInventoryLotNavigation      { get; set; }
    public ProductionOrder?  IdProductionOrderNavigation   { get; set; }
}
