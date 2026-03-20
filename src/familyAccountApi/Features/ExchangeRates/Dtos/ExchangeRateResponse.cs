namespace FamilyAccountApi.Features.ExchangeRates.Dtos;

public sealed record ExchangeRateResponse(
    int IdExchangeRate,
    int IdCurrency,
    string CodeCurrency,
    string NameCurrency,
    DateOnly RateDate,
    decimal RateValue);