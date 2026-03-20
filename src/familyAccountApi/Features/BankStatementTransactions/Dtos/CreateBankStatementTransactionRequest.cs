using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankStatementTransactions.Dtos;

public sealed record CreateBankStatementTransactionRequest
{
    [Required]
    [Description("ID de la importación a la que pertenece")]
    public required int IdBankStatementImport { get; init; }

    [Required]
    [Description("Fecha contable de la transacción")]
    public required DateOnly AccountingDate { get; init; }

    [Required]
    [Description("Fecha real de ejecución")]
    public required DateOnly TransactionDate { get; init; }

    [Description("Hora de la transacción")]
    public TimeOnly? TransactionTime { get; init; }

    [StringLength(100)]
    [Description("Número de documento o referencia")]
    public string? DocumentNumber { get; init; }

    [Required, StringLength(500, MinimumLength = 1)]
    [Description("Descripción de la transacción")]
    public required string Description { get; init; }

    [Description("Monto de débito")]
    public decimal? DebitAmount { get; init; }

    [Description("Monto de crédito")]
    public decimal? CreditAmount { get; init; }

    [Description("Saldo resultante")]
    public decimal? Balance { get; init; }

    [Description("ID del asiento contable asociado")]
    public int? IdAccountingEntry { get; init; }
}
