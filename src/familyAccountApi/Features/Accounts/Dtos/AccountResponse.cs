namespace FamilyAccountApi.Features.Accounts.Dtos;

public sealed record AccountResponse(
    int     IdAccount,
    string  CodeAccount,
    string  NameAccount,
    string  TypeAccount,
    int     LevelAccount,
    int?    IdAccountParent,
    string? NameAccountParent,
    bool    AllowsMovements,
    bool    IsActive);
