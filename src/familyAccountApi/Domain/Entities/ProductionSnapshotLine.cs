namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductionSnapshotLine
{
    public int      IdProductionSnapshotLine { get; set; }
    public int      IdProductionSnapshot     { get; set; }
    /// <summary>FK a la línea de receta de origen. NULL si el operador agregó un insumo extra no previsto en la receta.</summary>
    public int?     IdProductRecipeLine      { get; set; }
    /// <summary>Snapshot directo del producto insumo (desacoplado de la línea de receta para sobrevivir cambios futuros).</summary>
    public int      IdProductInput           { get; set; }
    /// <summary>Cantidad teórica calculada: ProductRecipeLine.QuantityInput × (QuantityReal / QuantityCalculated). 0 si es insumo extra.</summary>
    public decimal  QuantityCalculated       { get; set; }
    /// <summary>Cantidad real usada por el operador en esta corrida.</summary>
    public decimal  QuantityReal             { get; set; }
    public int      SortOrder                { get; set; }

    public ProductionSnapshot  IdProductionSnapshotNavigation  { get; set; } = null!;
    public ProductRecipeLine?  IdProductRecipeLineNavigation   { get; set; }
    public Product             IdProductInputNavigation        { get; set; } = null!;
}
