namespace FamilyAccountApi.Domain.Entities;

public sealed class BankStatementTemplate
{
    public int IdBankStatementTemplate { get; set; }
    public string CodeTemplate { get; set; } = null!;
    public string NameTemplate { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string ColumnMappings { get; set; } = null!;
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Relaciones
    public ICollection<BankStatementImport> BankStatementImports { get; set; } = [];
}
