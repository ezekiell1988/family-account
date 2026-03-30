namespace FamilyAccountApi.Domain.Entities;

public sealed class BankMovementDocument
{
    public int      IdBankMovementDocument { get; set; }
    public int      IdBankMovement         { get; set; }
    public int?     IdPurchaseInvoice      { get; set; }  // FK opcional a la factura de compra vinculada
    public string   TypeDocument           { get; set; } = null!;  // "FacturaCompra" | "Recibo" | "Transferencia" | "Cheque" | "Otro"
    public string?  NumberDocument         { get; set; }
    public DateOnly DateDocument           { get; set; }
    public decimal  AmountDocument         { get; set; }
    public string?  DescriptionDocument    { get; set; }

    public BankMovement      IdBankMovementNavigation     { get; set; } = null!;
    public PurchaseInvoice?  IdPurchaseInvoiceNavigation  { get; set; }
}
