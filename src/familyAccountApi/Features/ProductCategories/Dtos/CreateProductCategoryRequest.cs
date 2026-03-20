using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductCategories.Dtos;

public sealed record CreateProductCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Nombre de la categoría del producto")]
    public required string NameProductCategory { get; init; }
}
