namespace FamilyAccountApi.Domain.Entities;

public sealed class BankStatementImport
{
    public int IdBankStatementImport { get; set; }
    public int IdBankAccount { get; set; }
    public int IdBankStatementTemplate { get; set; }
    public string FileName { get; set; } = null!;
    public DateTime ImportDate { get; set; }
    public int ImportedBy { get; set; }
    public string Status { get; set; } = null!;
    public int TotalTransactions { get; set; }
    public int ProcessedTransactions { get; set; }
    public string? ErrorMessage { get; set; }

    // Relaciones
    public BankAccount IdBankAccountNavigation { get; set; } = null!;
    public BankStatementTemplate IdBankStatementTemplateNavigation { get; set; } = null!;
    public User ImportedByNavigation { get; set; } = null!;
    public ICollection<BankStatementTransaction> BankStatementTransactions { get; set; } = [];
}
