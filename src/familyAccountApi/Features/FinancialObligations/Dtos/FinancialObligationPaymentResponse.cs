namespace FamilyAccountApi.Features.FinancialObligations.Dtos;

public sealed record FinancialObligationPaymentResponse(
    int      IdFinancialObligationPayment,
    int?     IdBankMovement,
    DateOnly DatePayment,
    decimal  AmountPaid,
    decimal  AmountCapitalPaid,
    decimal  AmountInterestPaid,
    decimal  AmountLatePaid,
    decimal  AmountOtherPaid,
    int?     IdAccountingEntry,
    bool     IsAutoProcessed,
    string?  Notes);
