namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductionSnapshot
{
    public int     IdProductionSnapshot  { get; set; }
    /// <summary>FK 1:1 al ajuste de inventario de tipo PRODUCCION.</summary>
    public int     IdInventoryAdjustment { get; set; }
    /// <summary>FK a la receta vigente al momento de la producción (snapshot de cuál estaba activa).</summary>
    public int     IdProductRecipe       { get; set; }
    /// <summary>Cantidad teórica del producto final según la receta (ProductRecipe.QuantityOutput al momento de confirmar).</summary>
    public decimal QuantityCalculated    { get; set; }
    /// <summary>Cantidad real producida fisicamente en esta corrida.</summary>
    public decimal QuantityReal          { get; set; }
    public DateTime CreatedAt            { get; set; }

    public InventoryAdjustment IdInventoryAdjustmentNavigation { get; set; } = null!;
    public ProductRecipe       IdProductRecipeNavigation       { get; set; } = null!;
    public ICollection<ProductionSnapshotLine> ProductionSnapshotLines { get; set; } = [];
}
