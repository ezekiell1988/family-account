namespace FamilyAccountApi.Domain.Entities;

public sealed class InventoryAdjustmentType
{
    public int    IdInventoryAdjustmentType   { get; set; }
    public string CodeInventoryAdjustmentType { get; set; } = null!;
    public string NameInventoryAdjustmentType { get; set; } = null!;

    /// <summary>Cuenta de activo de inventario: DR en entradas (delta+), CR en salidas (delta-).</summary>
    public int? IdAccountInventoryDefault   { get; set; }

    /// <summary>Cuenta contrapartida para entradas (delta &gt; 0) o ajustes de costo al alza: CR del asiento.</summary>
    public int? IdAccountCounterpartEntry   { get; set; }

    /// <summary>Cuenta contrapartida para salidas (delta &lt; 0) o ajustes de costo a la baja: DR del asiento.</summary>
    public int? IdAccountCounterpartExit    { get; set; }

    public bool IsActive { get; set; }

    public Account? IdAccountInventoryDefaultNavigation  { get; set; }
    public Account? IdAccountCounterpartEntryNavigation  { get; set; }
    public Account? IdAccountCounterpartExitNavigation   { get; set; }

    public ICollection<InventoryAdjustment> InventoryAdjustments { get; set; } = [];
}
