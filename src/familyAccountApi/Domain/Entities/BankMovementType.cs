namespace FamilyAccountApi.Domain.Entities;

public sealed class BankMovementType
{
    public int    IdBankMovementType   { get; set; }
    public string CodeBankMovementType { get; set; } = null!;
    public string NameBankMovementType { get; set; } = null!;
    public int    IdAccountCounterpart { get; set; }
    public string MovementSign         { get; set; } = null!;  // "Cargo" | "Abono"
    public bool   IsActive             { get; set; } = true;

    public Account IdAccountCounterpartNavigation { get; set; } = null!;
    public ICollection<BankMovement> BankMovements { get; set; } = [];
}
