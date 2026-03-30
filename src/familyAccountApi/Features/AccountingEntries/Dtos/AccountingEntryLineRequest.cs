using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.AccountingEntries.Dtos;

public sealed record AccountingEntryLineRequest
{
    [Required]
    [Description("Cuenta contable afectada por la línea del asiento")]
    public required int IdAccount { get; init; }

    [Range(typeof(decimal), "0", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Monto del débito. Debe ser 0 cuando la línea sea de crédito")]
    public decimal DebitAmount { get; init; }

    [Range(typeof(decimal), "0", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Monto del crédito. Debe ser 0 cuando la línea sea de débito")]
    public decimal CreditAmount { get; init; }

    [StringLength(300)]
    [Description("Descripción opcional de la línea del asiento")]
    public string? DescriptionLine { get; init; }
}
