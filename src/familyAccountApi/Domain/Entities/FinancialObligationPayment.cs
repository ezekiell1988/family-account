namespace FamilyAccountApi.Domain.Entities;

public sealed class FinancialObligationPayment
{
    public int      IdFinancialObligationPayment     { get; set; }
    public int      IdFinancialObligationInstallment { get; set; }
    public int?     IdBankMovement                   { get; set; }   // FK → bankMovement (auto o manual)
    public DateOnly DatePayment                      { get; set; }
    public decimal  AmountPaid                       { get; set; }
    public decimal  AmountCapitalPaid                { get; set; }
    public decimal  AmountInterestPaid               { get; set; }
    public decimal  AmountLatePaid                   { get; set; }   // Default 0
    public decimal  AmountOtherPaid                  { get; set; }   // Default 0
    public int?     IdAccountingEntry                { get; set; }   // FK → asiento generado
    public bool     IsAutoProcessed                  { get; set; }   // true = generado por sync-excel
    public string?  Notes                            { get; set; }

    public FinancialObligationInstallment IdFinancialObligationInstallmentNavigation { get; set; } = null!;
    public BankMovement?                  IdBankMovementNavigation                   { get; set; }
    public AccountingEntry?               IdAccountingEntryNavigation                { get; set; }
}
