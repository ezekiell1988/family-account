using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.ProductAccounts.Dtos;

public sealed record UpdateProductAccountRequest
{
    [Required, Range(1, int.MaxValue)]
    [Description("ID de la cuenta contable de gasto (DR del asiento)")]
    public required int IdAccount { get; init; }

    [Range(1, int.MaxValue)]
    [Description("ID del centro de costo (opcional)")]
    public int? IdCostCenter { get; init; }

    [Required]
    [Range(typeof(decimal), "-100.00", "100.00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Porcentaje del total de la línea asignado a esta cuenta/centro de costo. La suma por producto debe ser 100.00.")]
    public required decimal PercentageAccount { get; init; }
}
