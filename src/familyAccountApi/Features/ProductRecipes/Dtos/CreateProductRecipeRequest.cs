using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductRecipes.Dtos;

public sealed record ProductRecipeLineRequest
{
    [Required]
    [Description("ID del producto insumo (no puede ser igual al producto output de la receta)")]
    public required int IdProductInput { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "999999999999.9999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cantidad del insumo en su unidad base por cada corrida")]
    public required decimal QuantityInput { get; init; }

    [Description("Orden de visualización del ingrediente")]
    public int SortOrder { get; init; } = 0;
}

public sealed record CreateProductRecipeRequest
{
    [Required]
    [Description("ID del producto que produce esta receta (no puede ser Materia Prima ni Reventa)")]
    public required int IdProductOutput { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre descriptivo de la receta")]
    public required string NameRecipe { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "999999999999.9999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Cantidad producida por corrida en unidad base del output")]
    public required decimal QuantityOutput { get; init; }

    [StringLength(500)]
    [Description("Instrucciones generales del proceso productivo")]
    public string? DescriptionRecipe { get; init; }

    [Required]
    [Description("Ingredientes de la receta")]
    public required IReadOnlyList<ProductRecipeLineRequest> Lines { get; init; }
}
