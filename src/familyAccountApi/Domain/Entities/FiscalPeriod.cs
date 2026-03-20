namespace FamilyAccountApi.Domain.Entities;

public sealed class FiscalPeriod
{
    public int     IdFiscalPeriod { get; set; }
    public int     YearPeriod     { get; set; }
    public int     MonthPeriod    { get; set; }
    public string  NamePeriod     { get; set; } = null!;
    public string  StatusPeriod   { get; set; } = null!;
    public DateOnly StartDate     { get; set; }
    public DateOnly EndDate       { get; set; }

    public ICollection<AccountingEntry> AccountingEntries { get; set; } = [];
}
