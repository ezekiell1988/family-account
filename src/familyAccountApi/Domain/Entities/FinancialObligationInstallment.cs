namespace FamilyAccountApi.Domain.Entities;

public sealed class FinancialObligationInstallment
{
    public int       IdFinancialObligationInstallment { get; set; }
    public int       IdFinancialObligation            { get; set; }
    public int       NumberInstallment                { get; set; }   // Clave natural del Excel
    public DateOnly  DueDate                          { get; set; }
    public decimal   BalanceAfter                     { get; set; }   // Saldo luego del pago
    public decimal   AmountCapital                    { get; set; }
    public decimal   AmountInterest                   { get; set; }
    public decimal   AmountLateFee                    { get; set; }   // Default 0
    public decimal   AmountOther                      { get; set; }   // Default 0
    public decimal   AmountTotal                      { get; set; }   // Capital + Interés + Mora + Otros
    public string    StatusInstallment                { get; set; } = null!;  // "Pendiente" | "Vigente" | "Pagada" | "Vencida"
    public DateTime? SyncedAt                         { get; set; }   // Última sincronización desde Excel

    public FinancialObligation         IdFinancialObligationNavigation { get; set; } = null!;
    public FinancialObligationPayment? FinancialObligationPayment      { get; set; }
}
