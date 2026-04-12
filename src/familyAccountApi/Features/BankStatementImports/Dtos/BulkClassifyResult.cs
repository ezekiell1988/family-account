namespace FamilyAccountApi.Features.BankStatementImports.Dtos;

/// <summary>
/// Resultado de la clasificación masiva de un import.
/// </summary>
public sealed record BulkClassifyResult(
    int Classified,
    int KeywordsAdded);
