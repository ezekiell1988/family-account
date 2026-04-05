namespace FamilyAccountApi.Domain.Entities;

public sealed class BankMovementDocument
{
    public int      IdBankMovementDocument { get; set; }
    public int      IdBankMovement         { get; set; }
    public int?     IdPurchaseInvoice      { get; set; }  // FK opcional a la factura de compra vinculada
    public int?     IdSalesInvoice         { get; set; }  // FK opcional a la factura de venta vinculada
    public string   TypeDocument           { get; set; } = null!;  // "FacturaCompra" | "FacturaVenta" | "Recibo" | "Transferencia" | "Cheque" | "Otro"
    public string?  NumberDocument         { get; set; }
    public DateOnly DateDocument           { get; set; }
    public decimal  AmountDocument         { get; set; }
    public string?  DescriptionDocument    { get; set; }

    public BankMovement      IdBankMovementNavigation     { get; set; } = null!;
    public PurchaseInvoice?  IdPurchaseInvoiceNavigation  { get; set; }
    public SalesInvoice?     IdSalesInvoiceNavigation     { get; set; }
}
