namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductionOrderLine
{
    public int      IdProductionOrderLine { get; set; }
    public int      IdProductionOrder     { get; set; }
    public int      IdProduct             { get; set; }   // Producto final a producir
    public int      IdProductUnit         { get; set; }   // Unidad de producción
    public int?     IdSalesOrderLine      { get; set; }   // Trazabilidad: línea del pedido que origina esta producción
    public decimal  QuantityRequired      { get; set; }   // Cantidad comprometida en unidad base
    public decimal  QuantityProduced      { get; set; }   // Acumulado de lo producido
    public string?  DescriptionLine       { get; set; }

    public ProductionOrder  IdProductionOrderNavigation { get; set; } = null!;
    public Product          IdProductNavigation         { get; set; } = null!;
    public ProductUnit      IdProductUnitNavigation     { get; set; } = null!;
    public SalesOrderLine?  IdSalesOrderLineNavigation  { get; set; }
}
