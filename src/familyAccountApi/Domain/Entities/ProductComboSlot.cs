namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductComboSlot
{
    public int     IdProductComboSlot { get; set; }
    public int     IdProductCombo     { get; set; }
    public string  NameSlot           { get; set; } = null!;
    public decimal Quantity           { get; set; }
    public bool    IsRequired         { get; set; }
    public int     SortOrder          { get; set; }

    public Product IdProductComboNavigation { get; set; } = null!;

    public ICollection<ProductComboSlotProduct>       ProductComboSlotProducts { get; set; } = [];
    public ICollection<ProductComboSlotPresetOption>    PresetOptions            { get; set; } = [];
}
