using System.Globalization;
using System.Text;

namespace FamilyAccountApi.Features.BankStatementImports.Parsers;

/// <summary>
/// Parsea el formato CSV punto-y-coma que exporta el Banco Nacional de Costa Rica (BNCR).
/// <para>
/// Formato de columnas:
/// <c>oficina ; fechaMovimiento ; numeroDocumento ; debito ; credito ; descripcion ;</c>
/// </para>
/// El parser omite automáticamente la fila de encabezado y la fila de totales
/// identificando las filas donde <c>fechaMovimiento</c> no es una fecha válida.
/// </summary>
public sealed class BncrCsvParser : IBankStatementParser
{
    public IReadOnlyList<ParsedTransaction> Parse(
        Stream  fileStream,
        string  columnMappingsJson,
        string? dateFormat = null,
        string? timeFormat = null)
    {
        var dateFmt = dateFormat ?? "dd/MM/yyyy";
        var transactions = new List<ParsedTransaction>();

        // El BNCR exporta en Windows-1252 / Latin-1.
        using var reader = new StreamReader(
            fileStream, Encoding.Latin1,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Dividir por ';' — puede haber campo vacío al final (trailing semicolon).
            var cols = line.Split(';');
            if (cols.Length < 6) continue;

            // col[1] = fechaMovimiento — si no es fecha, saltar (encabezado / total).
            var dateStr = cols[1].Trim();
            if (!DateOnly.TryParseExact(dateStr, dateFmt,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;

            var docNum      = cols[2].Trim();
            var description = cols[5].Trim();
            if (string.IsNullOrWhiteSpace(description)) continue;

            var debitAmt  = ParseAmount(cols[3].Trim());
            var creditAmt = ParseAmount(cols[4].Trim());

            // Fila sin monto → saltar (puede ser subtítulo intercalado).
            if ((debitAmt is null or 0m) && (creditAmt is null or 0m)) continue;

            transactions.Add(new ParsedTransaction(
                AccountingDate:  date,
                TransactionDate: date,
                TransactionTime: null,
                DocumentNumber:  string.IsNullOrWhiteSpace(docNum) ? null : docNum,
                Description:     description,
                DebitAmount:     debitAmt > 0m  ? debitAmt  : null,
                CreditAmount:    creditAmt > 0m ? creditAmt : null,
                Balance:         null));
        }

        return transactions;
    }

    /// <summary>
    /// Parsea montos detectando automáticamente el formato:
    ///   US/EN: 1,234.56  |  CR/EU: 1.234,56
    /// </summary>
    private static decimal? ParseAmount(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var cleaned = value
            .Replace("\u00a0", "")
            .Replace(" ", "")
            .TrimStart('¢', '$', '₡');

        if (string.IsNullOrEmpty(cleaned)) return null;

        int lastDot   = cleaned.LastIndexOf('.');
        int lastComma = cleaned.LastIndexOf(',');

        string normalized;
        if (lastDot > lastComma)
            normalized = cleaned.Replace(",", "");
        else if (lastComma > lastDot)
            normalized = cleaned.Replace(".", "").Replace(",", ".");
        else
            normalized = cleaned;

        return decimal.TryParse(normalized, NumberStyles.Number,
            CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}
