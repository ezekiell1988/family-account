using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductTypes.Dtos;

public sealed record CreateProductTypeRequest
{
    [Required, StringLength(60, MinimumLength = 1)]
    [Description("Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa")]
    public required string NameProductType { get; init; }

    [StringLength(300)]
    [Description("Descripción del tipo de producto y sus reglas de negocio")]
    public string? DescriptionProductType { get; init; }
}
