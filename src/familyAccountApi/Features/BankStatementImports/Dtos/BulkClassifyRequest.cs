using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankStatementImports.Dtos;

/// <summary>
/// Un ítem dentro del bulk classify: clasifica una transacción y opcionalmente aprende su keyword.
/// </summary>
public sealed record BulkClassifyItem
{
    [Required, Range(1, int.MaxValue)]
    [Description("ID de la transacción bancaria a clasificar")]
    public required int IdBankStatementTransaction { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("ID del tipo de movimiento bancario")]
    public required int IdBankMovementType { get; init; }

    [Description("ID de la cuenta contable contrapartida (sobreescribe la del tipo). Null = usar la del tipo.")]
    public int? IdAccountCounterpart { get; init; }

    [Description("Si true, la descripción de la transacción se agrega como keyword al template para uso futuro.")]
    public bool LearnKeyword { get; init; } = false;
}

/// <summary>
/// Solicitud de clasificación masiva para todas las transacciones de un import.
/// </summary>
public sealed record BulkClassifyRequest
{
    [Required, MinLength(1)]
    [Description("Lista de transacciones con su clasificación manual")]
    public required IReadOnlyList<BulkClassifyItem> Items { get; init; }
}
