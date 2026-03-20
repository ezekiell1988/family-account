namespace FamilyAccountApi.Features.FiscalPeriods.Dtos;

public sealed record FiscalPeriodResponse(
    int      IdFiscalPeriod,
    int      YearPeriod,
    int      MonthPeriod,
    string   NamePeriod,
    string   StatusPeriod,
    DateOnly StartDate,
    DateOnly EndDate);
