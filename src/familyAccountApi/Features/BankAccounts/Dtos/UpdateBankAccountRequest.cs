using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankAccounts.Dtos;

public sealed record UpdateBankAccountRequest
{
    [Required, Range(1, int.MaxValue)]
    [Description("Banco o institución financiera")]
    public required int IdBank { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("Cuenta contable vinculada a la cuenta bancaria")]
    public required int IdAccount { get; init; }

    [Required, Range(1, int.MaxValue)]
    [Description("Moneda de la cuenta bancaria")]
    public required int IdCurrency { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Código interno de la cuenta bancaria")]
    public required string CodeBankAccount { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    [Description("Número de cuenta bancaria o IBAN")]
    public required string AccountNumber { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Titular de la cuenta bancaria")]
    public required string AccountHolder { get; init; }

    [Description("Indica si la cuenta bancaria está activa")]
    public required bool IsActive { get; init; }
}