using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankStatementImports.Dtos;

public sealed record CreateBankStatementImportRequest
{
    [Required]
    [Description("ID de la cuenta bancaria")]
    public required int IdBankAccount { get; init; }

    [Required]
    [Description("ID de la plantilla de carga")]
    public required int IdBankStatementTemplate { get; init; }

    [Required, StringLength(500, MinimumLength = 1)]
    [Description("Nombre del archivo Excel")]
    public required string FileName { get; init; }

    [Description("Estado de la importación")]
    public string Status { get; init; } = "Pending";

    [Description("Total de transacciones en el archivo")]
    public int TotalTransactions { get; init; } = 0;

    [StringLength(2000)]
    [Description("Mensaje de error si falla la importación")]
    public string? ErrorMessage { get; init; }
}
