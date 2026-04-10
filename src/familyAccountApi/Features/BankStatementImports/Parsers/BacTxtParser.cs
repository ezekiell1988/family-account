using System.Globalization;
using System.Text;

namespace FamilyAccountApi.Features.BankStatementImports.Parsers;

/// <summary>
/// Parsea el formato TXT pipe-delimitado que exporta BAC Credomatic para
/// estados de cuenta de tarjetas de crédito.
/// <para>
/// Estructura esperada del archivo:
/// <list type="bullet">
///   <item>Encabezados de producto/titular (saltados hasta encontrar fila de datos).</item>
///   <item>Separadores <c>====</c> y <c>----</c>.</item>
///   <item>Fila de encabezado de datos: <c>Date | (descripción) | Local | Dollars</c>.</item>
///   <item>Filas de transacciones: <c>dd/MM/yyyy | descripción | monto_local | monto_usd</c>.</item>
/// </list>
/// </para>
/// Montos positivos = cargo (DebitAmount); negativos = pago recibido (CreditAmount).
/// Se prioriza monto local; si es cero, se usa monto USD.
/// </summary>
public sealed class BacTxtParser : IBankStatementParser
{
    public IReadOnlyList<ParsedTransaction> Parse(
        Stream  fileStream,
        string  columnMappingsJson,
        string? dateFormat = null,
        string? timeFormat = null)
    {
        var dateFmt = dateFormat ?? "dd/MM/yyyy";
        var transactions = new List<ParsedTransaction>();

        // BAC exporta en codificación compatible con Latin-1/Windows-1252.
        using var reader = new StreamReader(
            fileStream, Encoding.Latin1,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        bool inDataSection = false;
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            // Las columnas están separadas por '|' con espacios de relleno.
            var cols = line.Split('|');

            var col0 = cols.Length > 0 ? cols[0].Trim() : string.Empty;

            // ── Detectar inicio de sección de datos ─────────────────────────
            if (!inDataSection)
            {
                if (col0.Equals("Date", StringComparison.OrdinalIgnoreCase))
                    inDataSection = true;
                continue;
            }

            // ── Saltar separadores (---- y ====) ────────────────────────────
            if (col0.StartsWith("----", StringComparison.Ordinal) ||
                col0.StartsWith("====", StringComparison.Ordinal))
                continue;

            // ── Saltar filas sin fecha válida (saldos previos, totales, etc.) ─
            if (string.IsNullOrWhiteSpace(col0)) continue;

            if (!DateOnly.TryParseExact(col0, dateFmt,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;

            // ── Leer descripción ─────────────────────────────────────────────
            var description = cols.Length > 1 ? cols[1].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(description)) continue;

            // ── Leer montos (Local CRC | Dólares USD) ───────────────────────
            var localStr = cols.Length > 2 ? cols[2].Trim() : string.Empty;
            var usdStr   = cols.Length > 3 ? cols[3].Trim() : string.Empty;

            var localAmt = ParseAmount(localStr);
            var usdAmt   = ParseAmount(usdStr);

            // Priorizar monto local; si es cero usar USD.
            var amount = (localAmt.HasValue && localAmt.Value != 0m) ? localAmt : usdAmt;
            if (amount is null || amount == 0m) continue;

            // Cargo (+) → DebitAmount  |  Pago recibido (−) → CreditAmount.
            decimal? debitAmount  = amount > 0m ? amount        : null;
            decimal? creditAmount = amount < 0m ? Math.Abs(amount.Value) : null;

            transactions.Add(new ParsedTransaction(
                AccountingDate:  date,
                TransactionDate: date,
                TransactionTime: null,
                DocumentNumber:  null,
                Description:     description,
                DebitAmount:     debitAmount,
                CreditAmount:    creditAmount,
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
