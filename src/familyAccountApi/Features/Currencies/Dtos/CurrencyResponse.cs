namespace FamilyAccountApi.Features.Currencies.Dtos;

public sealed record CurrencyResponse(
    int IdCurrency,
    string CodeCurrency,
    string NameCurrency,
    string SymbolCurrency);