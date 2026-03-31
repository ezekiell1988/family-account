namespace FamilyAccountApi.Domain.Entities;

public sealed class PurchaseInvoiceType
{
    public int     IdPurchaseInvoiceType        { get; set; }
    public string  CodePurchaseInvoiceType      { get; set; } = null!;
    public string  NamePurchaseInvoiceType      { get; set; } = null!;
    public bool    CounterpartFromBankMovement  { get; set; }
    public int?    IdAccountCounterpartCRC      { get; set; }
    public int?    IdAccountCounterpartUSD      { get; set; }
    public int?    IdBankMovementType           { get; set; }  // Solo aplica cuando CounterpartFromBankMovement = true
    public int?    IdDefaultExpenseAccount      { get; set; }  // Cuenta de gasto fallback cuando el SKU no tiene ProductAccount configurado
    public bool    IsActive                     { get; set; }

    public Account?         IdAccountCounterpartCRCNavigation     { get; set; }
    public Account?         IdAccountCounterpartUSDNavigation     { get; set; }
    public BankMovementType? IdBankMovementTypeNavigation         { get; set; }
    public Account?         IdDefaultExpenseAccountNavigation     { get; set; }
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = [];
}
