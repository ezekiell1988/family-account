namespace FamilyAccountApi.Domain.Entities;

public sealed class Budget
{
    public int      IdBudget       { get; set; }
    public int      IdAccount      { get; set; }
    public int      IdFiscalPeriod { get; set; }
    public decimal  AmountBudget   { get; set; }
    public string?  NotesBudget    { get; set; }
    public bool     IsActive       { get; set; } = true;

    public Account      IdAccountNavigation      { get; set; } = null!;
    public FiscalPeriod IdFiscalPeriodNavigation { get; set; } = null!;
}