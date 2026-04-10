namespace FamilyAccountApi.Features.BankStatementImports.Parsers;

/// <summary>
/// Transacción normalizada que devuelve cualquier parser de extractos bancarios.
/// </summary>
public sealed record ParsedTransaction(
    DateOnly  AccountingDate,
    DateOnly  TransactionDate,
    TimeOnly? TransactionTime,
    string?   DocumentNumber,
    string    Description,
    decimal?  DebitAmount,
    decimal?  CreditAmount,
    decimal?  Balance);

/// <summary>
/// Contrato que deben implementar todos los parsers de extractos bancarios.
/// Cada implementación soporta un formato específico (HTML-XLS, TXT, CSV, etc.).
/// </summary>
public interface IBankStatementParser
{
    /// <param name="fileStream">Stream del archivo cargado (no se cierra internamente).</param>
    /// <param name="columnMappingsJson">JSON con configuración extra de columnas almacenado en el template.</param>
    /// <param name="dateFormat">Formato de fecha (ej. "dd/MM/yyyy"). Null usa el default del parser.</param>
    /// <param name="timeFormat">Formato de hora (ej. "HH:mm:ss"). Null usa el default o se omite.</param>
    IReadOnlyList<ParsedTransaction> Parse(
        Stream  fileStream,
        string  columnMappingsJson,
        string? dateFormat = null,
        string? timeFormat = null);
}
