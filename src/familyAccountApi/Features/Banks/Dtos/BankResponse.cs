namespace FamilyAccountApi.Features.Banks.Dtos;

public sealed record BankResponse(
    int    IdBank,
    string CodeBank,
    string NameBank,
    bool   IsActive);
