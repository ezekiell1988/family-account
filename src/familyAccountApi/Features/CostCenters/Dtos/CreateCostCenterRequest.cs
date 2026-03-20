using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.CostCenters.Dtos;

public sealed record CreateCostCenterRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Código único del centro de costo")]
    public required string CodeCostCenter { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre del centro de costo")]
    public required string NameCostCenter { get; init; }

    [Description("Indica si el centro de costo está activo")]
    public bool IsActive { get; init; } = true;
}
