namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesOrderLineSlotOption
{
    public int     IdSalesOrderLineSlotOption            { get; set; }
    public int     IdSalesOrderLineComboSlotSelection    { get; set; }
    public int     IdProductOptionItem                   { get; set; }
    public decimal Quantity                              { get; set; } = 1m;
    public bool    IsPreset                              { get; set; } = false;   // true = copiado del preset del slot

    public SalesOrderLineComboSlotSelection IdSalesOrderLineComboSlotSelectionNavigation { get; set; } = null!;
    public ProductOptionItem                IdProductOptionItemNavigation                { get; set; } = null!;
}
