using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankStatementImports.Dtos;

public sealed record UpdateBankStatementImportRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    [Description("Estado de la importación")]
    public required string Status { get; init; }

    [Description("Total de transacciones en el archivo")]
    public int TotalTransactions { get; init; }

    [Description("Transacciones procesadas exitosamente")]
    public int ProcessedTransactions { get; init; }

    [StringLength(2000)]
    [Description("Mensaje de error si falla la importación")]
    public string? ErrorMessage { get; init; }
}
