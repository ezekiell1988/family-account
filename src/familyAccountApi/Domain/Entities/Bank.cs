namespace FamilyAccountApi.Domain.Entities;

public sealed class Bank
{
    public int    IdBank    { get; set; }
    public string CodeBank  { get; set; } = null!;
    public string NameBank  { get; set; } = null!;
    public bool   IsActive  { get; set; } = true;

    public ICollection<BankAccount> BankAccounts { get; set; } = [];
}
