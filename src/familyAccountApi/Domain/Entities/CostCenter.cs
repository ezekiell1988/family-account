namespace FamilyAccountApi.Domain.Entities;

public sealed class CostCenter
{
    public int    IdCostCenter   { get; set; }
    public string CodeCostCenter { get; set; } = null!;
    public string NameCostCenter { get; set; } = null!;
    public bool   IsActive       { get; set; }

    // Navegación inversa desde accountingEntryLine
    public ICollection<AccountingEntryLine> AccountingEntryLines { get; set; } = [];
}
