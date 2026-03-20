namespace FamilyAccountApi.Features.BankMovementTypes.Dtos;

public sealed record BankMovementTypeResponse(
    int    IdBankMovementType,
    string CodeBankMovementType,
    string NameBankMovementType,
    int    IdAccountCounterpart,
    string CodeAccountCounterpart,
    string NameAccountCounterpart,
    string MovementSign,
    bool   IsActive);
