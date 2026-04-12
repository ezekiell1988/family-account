using ExcelDataReader;
using System.Globalization;

namespace FamilyAccountApi.Features.BankStatementImports.Parsers;

/// <summary>
/// Parsea el formato XLS (BIFF8) que exporta BAC Credomatic para cuentas de ahorro/débito.
/// <para>
/// Estructura del archivo:
/// <list type="bullet">
///   <item>Filas de encabezado con nombre del titular, número de producto y saldos.</item>
///   <item>Fila de cabecera de columnas: <c>Fecha | Referencia | | Código | Descripción | | | Débitos | Créditos | Balance*</c></item>
///   <item>Fila de saldo inicial (descripción = "Saldo Inicial", sin fecha válida).</item>
///   <item>Filas de transacciones: fecha dd/MM/yyyy, referencia, código, descripción, débitos (a cargo), créditos (a favor), balance.</item>
/// </list>
/// </para>
/// Columnas (0-indexadas):
///   0=Fecha  1=Referencia  3=Código  4=Descripción  7=Débitos  8=Créditos  9=Balance
/// </summary>
public sealed class BacXlsParser : IBankStatementParser
{
    public IReadOnlyList<ParsedTransaction> Parse(
        Stream  fileStream,
        string  columnMappingsJson,
        string? dateFormat = null,
        string? timeFormat = null)
    {
        var dateFmt      = dateFormat ?? "dd/MM/yyyy";
        var transactions = new List<ParsedTransaction>();

        using var reader = ExcelReaderFactory.CreateReader(fileStream,
            new ExcelReaderConfiguration { FallbackEncoding = System.Text.Encoding.Latin1 });

        bool inDataSection = false;

        while (reader.Read())
        {
            // ── Detectar fila de cabecera de columnas ───────────────────────
            if (!inDataSection)
            {
                var h = reader.GetValue(0)?.ToString()?.Trim() ?? string.Empty;
                if (h.Equals("Fecha", StringComparison.OrdinalIgnoreCase))
                    inDataSection = true;
                continue;
            }

            // ── Parsear fecha (col 0) ────────────────────────────────────────
            DateOnly date;
            var cellDate = reader.GetValue(0);
            if (cellDate is DateTime dt)
            {
                date = DateOnly.FromDateTime(dt);
            }
            else
            {
                var dateStr = cellDate?.ToString()?.Trim() ?? string.Empty;
                if (!DateOnly.TryParseExact(dateStr, dateFmt,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    continue; // fila sin fecha válida (saldo inicial, total, vacía)
            }

            var reference   = reader.GetValue(1)?.ToString()?.Trim();
            var description = reader.GetValue(4)?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(description)) continue;

            var debit   = GetDecimal(reader.GetValue(7));
            var credit  = GetDecimal(reader.GetValue(8));
            var balance = GetDecimal(reader.GetValue(9));

            // Ignora filas sin movimiento
            if ((debit is null or 0m) && (credit is null or 0m)) continue;

            transactions.Add(new ParsedTransaction(
                AccountingDate:  date,
                TransactionDate: date,
                TransactionTime: null,
                DocumentNumber:  string.IsNullOrWhiteSpace(reference) ? null : reference,
                Description:     description,
                DebitAmount:     debit  is > 0m ? debit  : null,
                CreditAmount:    credit is > 0m ? credit : null,
                Balance:         balance));
        }

        return transactions;
    }

    private static decimal? GetDecimal(object? value) => value switch
    {
        null             => null,
        double  d        => d  == 0 ? null : (decimal)d,
        decimal dec      => dec == 0 ? null : dec,
        int     i        => i  == 0 ? null : (decimal)i,
        string  s        => decimal.TryParse(
                                s.Replace(",", "").Replace(" ", ""),
                                NumberStyles.Number,
                                CultureInfo.InvariantCulture, out var r)
                            ? (r == 0 ? null : r)
                            : null,
        _                => null
    };
}
