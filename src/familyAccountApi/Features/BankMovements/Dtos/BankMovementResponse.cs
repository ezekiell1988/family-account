namespace FamilyAccountApi.Features.BankMovements.Dtos;

public sealed record BankMovementResponse(
    int      IdBankMovement,
    int      IdBankAccount,
    string   CodeBankAccount,
    string   AccountNumber,
    int      IdBankMovementType,
    string   CodeBankMovementType,
    string   NameBankMovementType,
    string   MovementSign,
    int      IdFiscalPeriod,
    string   NameFiscalPeriod,
    string   NumberMovement,
    DateOnly DateMovement,
    string   DescriptionMovement,
    decimal  Amount,
    string   StatusMovement,
    string?  ReferenceMovement,
    decimal  ExchangeRateValue,
    DateTime CreatedAt,
    int?     IdAccountingEntry,
    IReadOnlyList<BankMovementDocumentResponse> Documents);
