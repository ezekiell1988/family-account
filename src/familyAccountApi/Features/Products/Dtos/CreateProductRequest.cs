using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Products.Dtos;

public sealed record CreateProductRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Código único del producto")]
    public required string CodeProduct { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre del producto")]
    public required string NameProduct { get; init; }
}
