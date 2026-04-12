namespace FamilyAccountApi.Domain.Entities;

public sealed class FinancialObligation
{
    public int      IdFinancialObligation  { get; set; }
    public string   NameObligation         { get; set; } = null!;
    public int      IdCurrency             { get; set; }
    public decimal  OriginalAmount         { get; set; }
    public decimal  InterestRate           { get; set; }   // Tasa anual Ej: 18.50
    public DateOnly StartDate              { get; set; }
    public int      TermMonths             { get; set; }
    public int?     IdBankAccountPayment   { get; set; }   // Cuenta BAC débito desde la que se paga
    public int      IdAccountLongTerm      { get; set; }   // Pasivo No Corriente Ej: 2.2.01.01
    public int      IdAccountShortTerm     { get; set; }   // Pasivo Corriente porción Ej: 2.1.02.01
    public int      IdAccountInterest      { get; set; }   // Gasto Intereses     Ej: 5.5.05
    public int?     IdAccountLateFee       { get; set; }   // Gasto Mora          Ej: 5.5.06
    public int?     IdAccountOther         { get; set; }   // Gasto Otros cargos
    public string   MatchKeyword           { get; set; } = null!;  // Ej: "COOPEALIANZA"
    public string   StatusObligation       { get; set; } = null!;  // "Activo" | "Liquidado"
    public string?  Notes                  { get; set; }

    public Currency                              IdCurrencyNavigation           { get; set; } = null!;
    public BankAccount?                          IdBankAccountPaymentNavigation { get; set; }
    public Account                               IdAccountLongTermNavigation    { get; set; } = null!;
    public Account                               IdAccountShortTermNavigation   { get; set; } = null!;
    public Account                               IdAccountInterestNavigation    { get; set; } = null!;
    public Account?                              IdAccountLateFeeNavigation     { get; set; }
    public Account?                              IdAccountOtherNavigation       { get; set; }
    public ICollection<FinancialObligationInstallment> FinancialObligationInstallments { get; set; } = [];
}
