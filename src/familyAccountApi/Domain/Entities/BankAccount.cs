namespace FamilyAccountApi.Domain.Entities;

public sealed class BankAccount
{
    public int    IdBankAccount     { get; set; }
    public int    IdAccount         { get; set; }
    public int    IdCurrency        { get; set; }
    public string CodeBankAccount   { get; set; } = null!;
    public string BankName          { get; set; } = null!;
    public string AccountNumber     { get; set; } = null!;
    public string AccountHolder     { get; set; } = null!;
    public bool   IsActive          { get; set; } = true;

    public Account  IdAccountNavigation  { get; set; } = null!;
    public Currency IdCurrencyNavigation { get; set; } = null!;
}