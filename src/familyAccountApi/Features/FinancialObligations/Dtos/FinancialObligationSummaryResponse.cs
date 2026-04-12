namespace FamilyAccountApi.Features.FinancialObligations.Dtos;

public sealed record FinancialObligationSummaryResponse(
    int      IdFinancialObligation,
    string   NameObligation,
    string   CodeCurrency,
    decimal  OriginalAmount,
    decimal  CurrentBalance,           // BalanceAfter de la última cuota Pagada (o OriginalAmount)
    decimal  TotalCapitalPaid,
    decimal  TotalInterestPaid,
    decimal  TotalLatePaid,
    decimal  PortionCurrentYear,       // Capital de cuotas con DueDate en los próximos 12 meses
    int?     CurrentInstallmentNumber,
    DateOnly? CurrentInstallmentDue,
    decimal? CurrentInstallmentTotal,
    int?     NextInstallmentNumber,
    DateOnly? NextInstallmentDue,
    decimal? NextInstallmentTotal,
    int      InstallmentsPaid,
    int      InstallmentsPending,
    string   StatusObligation);
