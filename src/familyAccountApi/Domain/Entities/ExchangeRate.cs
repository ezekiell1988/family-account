namespace FamilyAccountApi.Domain.Entities;

public sealed class ExchangeRate
{
    public int      IdExchangeRate { get; set; }
    public int      IdCurrency     { get; set; }
    public DateOnly RateDate       { get; set; }
    public decimal  RateValue      { get; set; }

    public Currency IdCurrencyNavigation { get; set; } = null!;
}