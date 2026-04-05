using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductRecipes.Dtos;

public sealed record UpdateProductRecipeRequest
{
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
    [Description("Ingredientes de la receta (reemplaza completamente las líneas existentes)")]
    public required IReadOnlyList<ProductRecipeLineRequest> Lines { get; init; }
}
