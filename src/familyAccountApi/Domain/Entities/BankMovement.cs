namespace FamilyAccountApi.Domain.Entities;

public sealed class BankMovement
{
    public int      IdBankMovement      { get; set; }
    public int      IdBankAccount       { get; set; }
    public int      IdBankMovementType  { get; set; }
    public int      IdFiscalPeriod      { get; set; }
    public string   NumberMovement      { get; set; } = null!;
    public DateOnly DateMovement        { get; set; }
    public string   DescriptionMovement { get; set; } = null!;
    public decimal  Amount              { get; set; }
    public string   StatusMovement      { get; set; } = null!;  // "Borrador" | "Confirmado" | "Anulado"
    public string?  ReferenceMovement   { get; set; }
    public decimal  ExchangeRateValue   { get; set; }
    public DateTime CreatedAt           { get; set; }

    public BankAccount      IdBankAccountNavigation      { get; set; } = null!;
    public BankMovementType IdBankMovementTypeNavigation { get; set; } = null!;
    public FiscalPeriod     IdFiscalPeriodNavigation     { get; set; } = null!;
    public ICollection<BankMovementDocument> BankMovementDocuments { get; set; } = [];
}
