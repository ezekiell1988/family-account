namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductComboSlotProduct
{
    public int     IdProductComboSlotProduct { get; set; }
    public int     IdProductComboSlot        { get; set; }
    public int     IdProduct                 { get; set; }
    public decimal PriceAdjustment           { get; set; }
    public int     SortOrder                 { get; set; }

    public ProductComboSlot IdProductComboSlotNavigation { get; set; } = null!;
    public Product          IdProductNavigation          { get; set; } = null!;
}
