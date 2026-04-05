namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesOrderLineComboSlotSelection
{
    public int IdSalesOrderLineComboSlotSelection { get; set; }
    public int IdSalesOrderLine                   { get; set; }
    public int IdProductComboSlot                 { get; set; }
    public int IdProduct                          { get; set; }   // Producto elegido por el cliente en el slot

    public SalesOrderLine    IdSalesOrderLineNavigation    { get; set; } = null!;
    public ProductComboSlot  IdProductComboSlotNavigation  { get; set; } = null!;
    public Product           IdProductNavigation           { get; set; } = null!;

    public ICollection<SalesOrderLineSlotOption> SalesOrderLineSlotOptions { get; set; } = [];
}
