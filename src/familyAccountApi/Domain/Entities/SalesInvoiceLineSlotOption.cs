namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesInvoiceLineSlotOption
{
    public int     IdSalesInvoiceLineSlotOption              { get; set; }
    public int     IdSalesInvoiceLineComboSlotSelection      { get; set; }
    public int     IdProductOptionItem                       { get; set; }
    public decimal Quantity                                  { get; set; } = 1m;
    public bool    IsPreset                                  { get; set; } = false;   // true = copiado del preset del slot

    public SalesInvoiceLineComboSlotSelection IdSalesInvoiceLineComboSlotSelectionNavigation { get; set; } = null!;
    public ProductOptionItem                  IdProductOptionItemNavigation                  { get; set; } = null!;
}
