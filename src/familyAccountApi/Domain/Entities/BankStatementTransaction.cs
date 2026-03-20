namespace FamilyAccountApi.Domain.Entities;

public sealed class BankStatementTransaction
{
    public int IdBankStatementTransaction { get; set; }
    public int IdBankStatementImport { get; set; }
    public DateOnly AccountingDate { get; set; }
    public DateOnly TransactionDate { get; set; }
    public TimeOnly? TransactionTime { get; set; }
    public string? DocumentNumber { get; set; }
    public string Description { get; set; } = null!;
    public decimal? DebitAmount { get; set; }
    public decimal? CreditAmount { get; set; }
    public decimal? Balance { get; set; }
    public bool IsReconciled { get; set; }
    public int? IdAccountingEntry { get; set; }

    // Relaciones
    public BankStatementImport IdBankStatementImportNavigation { get; set; } = null!;
    public AccountingEntry? IdAccountingEntryNavigation { get; set; }
}
