namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesInvoiceType
{
    public int     IdSalesInvoiceType           { get; set; }
    public string  CodeSalesInvoiceType         { get; set; } = null!;
    public string  NameSalesInvoiceType         { get; set; } = null!;
    public bool    CounterpartFromBankMovement  { get; set; }
    public int?    IdAccountCounterpartCRC      { get; set; }   // Caja CRC (si CounterpartFromBankMovement = false)
    public int?    IdAccountCounterpartUSD      { get; set; }   // Caja USD (si CounterpartFromBankMovement = false)
    public int?    IdBankMovementType           { get; set; }   // Solo si CounterpartFromBankMovement = true
    public int?    IdAccountSalesRevenue        { get; set; }   // Cuenta CR de ingresos (fallback por tipo)
    public int?    IdAccountCOGS               { get; set; }   // Cuenta DR costo de ventas
    public int?    IdAccountInventory           { get; set; }   // Cuenta CR inventario al reconocer costo
    public bool    IsActive                     { get; set; }

    public Account?         IdAccountCounterpartCRCNavigation  { get; set; }
    public Account?         IdAccountCounterpartUSDNavigation  { get; set; }
    public BankMovementType? IdBankMovementTypeNavigation      { get; set; }
    public Account?         IdAccountSalesRevenueNavigation    { get; set; }
    public Account?         IdAccountCOGSNavigation            { get; set; }
    public Account?         IdAccountInventoryNavigation       { get; set; }
    public ICollection<SalesInvoice> SalesInvoices { get; set; } = [];
}
