namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductComboSlotPresetOption
{
    public int IdProductComboSlotPresetOption { get; set; }
    public int IdProductComboSlot             { get; set; }
    public int IdProductOptionItem            { get; set; }

    public ProductComboSlot  IdProductComboSlotNavigation  { get; set; } = null!;
    public ProductOptionItem IdProductOptionItemNavigation { get; set; } = null!;
}
