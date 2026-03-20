namespace FamilyAccountApi.Domain.Entities;

public sealed class AccountingEntryLine
{
    public int     IdAccountingEntryLine { get; set; }
    public int     IdAccountingEntry     { get; set; }
    public int     IdAccount             { get; set; }
    public decimal DebitAmount           { get; set; }
    public decimal CreditAmount          { get; set; }
    public string? DescriptionLine       { get; set; }

    public AccountingEntry IdAccountingEntryNavigation { get; set; } = null!;
    public Account IdAccountNavigation { get; set; } = null!;
}
