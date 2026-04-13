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
    /// <summary>Tipo de movimiento bancario asignado (auto o manual). Null si aún no fue clasificado.</summary>
    public int? IdBankMovementType { get; set; }
    /// <summary>Cuenta contable contrapartida (sobrescribe la del tipo de movimiento). Null = usar la del tipo.</summary>
    public int? IdAccountCounterpart { get; set; }
    /// <summary>Centro de costo para la clasificación contable. Opcional.</summary>
    public int? IdCostCenter { get; set; }
    public int? IdAccountingEntry { get; set; }

    // Relaciones
    public BankStatementImport IdBankStatementImportNavigation { get; set; } = null!;
    public BankMovementType? IdBankMovementTypeNavigation { get; set; }
    public Account? IdAccountCounterpartNavigation { get; set; }
    public CostCenter? IdCostCenterNavigation { get; set; }
    public AccountingEntry? IdAccountingEntryNavigation { get; set; }
}
