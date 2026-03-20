namespace FamilyAccountApi.Domain.Entities;

public sealed class Currency
{
    public int    IdCurrency     { get; set; }
    public string CodeCurrency   { get; set; } = null!;
    public string NameCurrency   { get; set; } = null!;
    public string SymbolCurrency { get; set; } = null!;

    public ICollection<ExchangeRate> ExchangeRates { get; set; } = [];
    public ICollection<AccountingEntry> AccountingEntries { get; set; } = [];
    public ICollection<BankAccount> BankAccounts { get; set; } = [];
}