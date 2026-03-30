namespace FamilyAccountApi.Features.ProductAccounts.Dtos;

public sealed record ProductAccountResponse(
    int     IdProductAccount,
    int     IdProduct,
    string  CodeProduct,
    string  NameProduct,
    int     IdAccount,
    string  CodeAccount,
    string  NameAccount,
    int?    IdCostCenter,
    string? NameCostCenter,
    decimal PercentageAccount);
