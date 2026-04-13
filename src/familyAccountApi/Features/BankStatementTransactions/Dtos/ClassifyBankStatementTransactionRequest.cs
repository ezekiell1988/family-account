using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankStatementTransactions.Dtos;

/// <summary>
/// Permite clasificar (o reclasificar) manualmente una transacción de extracto bancario,
/// asignando el tipo de movimiento y opcionalmente la cuenta contrapartida.
/// </summary>
public sealed record ClassifyBankStatementTransactionRequest
{
    [Description("ID del tipo de movimiento bancario. Enviar null para limpiar la clasificación.")]
    public int? IdBankMovementType { get; init; }

    [Description("ID de la cuenta contable contrapartida (sobreescribe la del tipo de movimiento). " +
                 "Enviar null para usar la cuenta por defecto del tipo de movimiento.")]
    public int? IdAccountCounterpart { get; init; }

    [Description("ID del centro de costo (opcional).")]
    public int? IdCostCenter { get; init; }
}
