namespace FamilyAccountApi.Features.FinancialObligations.Dtos;

public sealed record FinancialObligationInstallmentResponse(
    int       IdFinancialObligationInstallment,
    int       NumberInstallment,
    DateOnly  DueDate,
    decimal   BalanceAfter,
    decimal   AmountCapital,
    decimal   AmountInterest,
    decimal   AmountLateFee,
    decimal   AmountOther,
    decimal   AmountTotal,
    string    StatusInstallment,
    DateTime? SyncedAt,
    FinancialObligationPaymentResponse? Payment);
