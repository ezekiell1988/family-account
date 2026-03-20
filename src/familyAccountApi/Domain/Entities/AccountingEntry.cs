namespace FamilyAccountApi.Domain.Entities;

public sealed class AccountingEntry
{
    public int      IdAccountingEntry { get; set; }
    public int      IdFiscalPeriod    { get; set; }
    public string   NumberEntry       { get; set; } = null!;
    public DateOnly DateEntry         { get; set; }
    public string   DescriptionEntry  { get; set; } = null!;
    public string   StatusEntry       { get; set; } = null!;
    public string?  ReferenceEntry    { get; set; }
    public DateTime CreatedAt         { get; set; }

    public FiscalPeriod IdFiscalPeriodNavigation { get; set; } = null!;
    public ICollection<AccountingEntryLine> AccountingEntryLines { get; set; } = [];
}
