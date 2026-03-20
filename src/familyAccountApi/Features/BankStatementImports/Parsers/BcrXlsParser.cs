using HtmlAgilityPack;
using System.Globalization;
using System.Text.Json;

namespace FamilyAccountApi.Features.BankStatementImports.Parsers;

/// <summary>
/// Mapeo de índices de columna para el parseo del extracto bancario.
/// Se almacena como JSON en BankStatementTemplate.ColumnMappings.
/// </summary>
public sealed record BcrColumnMapping
{
    public int AccountingDate  { get; init; } = 0;
    public int TransactionDate { get; init; } = 1;
    public int TransactionTime { get; init; } = 2;
    public int DocumentNumber  { get; init; } = 3;
    public int Description     { get; init; } = 4;
    public int DebitAmount     { get; init; } = 5;
    public int CreditAmount    { get; init; } = 6;
    public int Balance         { get; init; } = 7;
    /// <summary>Filas de encabezado a saltar en la tabla de datos.</summary>
    public int SkipHeaderRows  { get; init; } = 1;
}

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
/// Parsea el formato HTML-XLS que exporta el Banco de Costa Rica (BCR).
/// El archivo .xls es en realidad HTML con una tabla <c>id="t1"</c>.
/// </summary>
public static class BcrXlsParser
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<ParsedTransaction> Parse(
        Stream     fileStream,
        string     columnMappingsJson,
        string?    dateFormat = "dd/MM/yyyy",
        string?    timeFormat = "HH:mm:ss")
    {
        var mapping = JsonSerializer.Deserialize<BcrColumnMapping>(columnMappingsJson, JsonOpts)
                      ?? new BcrColumnMapping();

        var doc = new HtmlDocument();
        doc.Load(fileStream, detectEncodingFromByteOrderMarks: true);

        // El BCR usa id="t1" para la tabla de datos;
        // si no, toma la primera tabla que tenga encabezados.
        var table = doc.GetElementbyId("t1")
                    ?? doc.DocumentNode.SelectSingleNode("//table[.//th]");

        if (table is null)
            throw new InvalidOperationException(
                "No se encontró la tabla de datos en el archivo. " +
                "Verifique que sea un extracto BCR en formato HTML/XLS.");

        var allRows = table.SelectNodes(".//tr");
        if (allRows is null || allRows.Count == 0)
            throw new InvalidOperationException("La tabla de datos del archivo está vacía.");

        var dataRows = allRows
            .Skip(mapping.SkipHeaderRows)
            .Where(r => r.SelectNodes(".//td") is not null)
            .ToList();

        var efFmt    = dateFormat ?? "dd/MM/yyyy";
        var timeFmt  = timeFormat ?? "HH:mm:ss";
        var transactions = new List<ParsedTransaction>(dataRows.Count);

        foreach (var row in dataRows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells is null || cells.Count < 5) continue;

            string Cell(int idx) =>
                idx < cells.Count
                    ? HtmlEntity.DeEntitize(cells[idx].InnerText.Trim())
                    : string.Empty;

            var accountingDateStr  = Cell(mapping.AccountingDate);
            var transactionDateStr = Cell(mapping.TransactionDate);

            // Fila de totales / vacía → ignorar
            if (string.IsNullOrWhiteSpace(accountingDateStr)) continue;

            if (!DateOnly.TryParseExact(accountingDateStr, efFmt,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var accountingDate))
                continue;

            if (!DateOnly.TryParseExact(transactionDateStr, efFmt,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var transactionDate))
                transactionDate = accountingDate;

            TimeOnly? time = null;
            var timeStr = Cell(mapping.TransactionTime);
            if (!string.IsNullOrWhiteSpace(timeStr))
            {
                // Algunos formatos de hora del BCR incluyen segundos, otros no
                if (TimeOnly.TryParseExact(timeStr, timeFmt,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
                    || TimeOnly.TryParseExact(timeStr, "HH:mm",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out t))
                    time = t;
            }

            var docNumRaw = Cell(mapping.DocumentNumber);
            var docNum    = string.IsNullOrWhiteSpace(docNumRaw) ? null : docNumRaw;
            var description = Cell(mapping.Description);
            if (string.IsNullOrWhiteSpace(description)) continue;

            var debit   = ParseAmount(Cell(mapping.DebitAmount));
            var credit  = ParseAmount(Cell(mapping.CreditAmount));
            var balance = ParseAmount(Cell(mapping.Balance));

            transactions.Add(new ParsedTransaction(
                accountingDate, transactionDate, time,
                docNum, description,
                debit, credit, balance));
        }

        return transactions;
    }

    /// <summary>
    /// Parsea montos en formato costarricense: 1.234,56
    /// (punto = separador de miles, coma = separador decimal).
    /// </summary>
    private static decimal? ParseAmount(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Quitar espacios y símbolo de moneda si lo hubiera
        var cleaned = value.Replace(" ", "").TrimStart('¢', '$', '₡');

        // Formato CR: 1.234,56 → quitar puntos, cambiar coma por punto
        cleaned = cleaned.Replace(".", "").Replace(",", ".");

        return decimal.TryParse(cleaned, NumberStyles.Number,
            CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}
