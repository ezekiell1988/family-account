using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Products.Dtos;

public sealed record GenerateVariantsRequest
{
    [Required]
    [Description("ID del tipo de producto para las variantes hijas")]
    public required int IdProductType { get; init; }

    [Required]
    [Description("ID de la unidad base de inventario para las variantes hijas")]
    public required int IdUnit { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Prefijo del código para las variantes generadas (ej: CAMISA-OXFORD)")]
    public required string CodePrefix { get; init; }
}

public sealed record VariantAttributeSummary(string NameAttribute, string NameValue);

public sealed record VariantSummary(
    int    IdProduct,
    string NameProduct,
    string CodeProduct,
    IReadOnlyList<VariantAttributeSummary> Attributes);

public sealed record GenerateVariantsResponse(
    int Created,
    int Skipped,
    IReadOnlyList<VariantSummary> Variants);
