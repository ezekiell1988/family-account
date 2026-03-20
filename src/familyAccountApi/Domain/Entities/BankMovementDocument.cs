namespace FamilyAccountApi.Domain.Entities;

public sealed class BankMovementDocument
{
    public int      IdBankMovementDocument { get; set; }
    public int      IdBankMovement         { get; set; }
    public int?     IdAccountingEntry      { get; set; }
    public string   TypeDocument           { get; set; } = null!;  // "Asiento" | "Factura" | "Recibo" | "Transferencia" | "Cheque" | "Otro"
    public string?  NumberDocument         { get; set; }
    public DateOnly DateDocument           { get; set; }
    public decimal  AmountDocument         { get; set; }
    public string?  DescriptionDocument    { get; set; }

    public BankMovement     IdBankMovementNavigation    { get; set; } = null!;
    public AccountingEntry? IdAccountingEntryNavigation { get; set; }
}
