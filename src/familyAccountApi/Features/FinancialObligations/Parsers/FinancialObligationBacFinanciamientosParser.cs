using ExcelDataReader;
using System.Globalization;

namespace FamilyAccountApi.Features.FinancialObligations.Parsers;

/// <summary>
/// Parsea el XLS (BIFF8) de "Consulta de Financiamientos" de BAC Credomatic.
///
/// Estructura del archivo (filas 0-indexed):
///   Filas 0-6: encabezados / metadatos → se saltan (7 filas)
///   Fila 7+  : datos, una fila = un plan de financiamiento
///
/// Columnas (0-indexed):
///   0: (ignorada — numeración o vacío)
///   1: Fecha de inicio / primer vencimiento  (string "15/07/2025" o serial OADate)
///   2: Concepto / descripción del financiamiento
///   3: Cuotas  "009/012"  (actual / total)
///   4: Monto cuota  "29,472.00 CRC"  (texto con moneda)
///   5: Saldo inicial  "353,664.79 CRC"
///   6: Saldo faltante  (no utilizado — se recalcula al generar la tabla)
/// </summary>
public sealed class FinancialObligationBacFinanciamientosParser
{
    private static readonly CultureInfo InvCulture = CultureInfo.InvariantCulture;

    public IReadOnlyList<BacFinanciamientoRow> Parse(Stream stream, string cardSuffix)
    {
        using var reader = ExcelReaderFactory.CreateReader(stream,
            new ExcelReaderConfiguration { FallbackEncoding = System.Text.Encoding.Latin1 });

        var result   = new List<BacFinanciamientoRow>();
        int rowIndex = 0;

        while (reader.Read())
        {
            rowIndex++;
            if (rowIndex <= 7) continue; // Saltar 7 filas de encabezado (Python: range(7, nrows))

            // Col 3: cuotas "009/012" — ancla principal para identificar filas de datos
            var rawCuotas = reader.GetValue(3)?.ToString()?.Trim();
            if (string.IsNullOrEmpty(rawCuotas) || !rawCuotas.Contains('/')) continue;

            var parts = rawCuotas.Split('/');
            if (parts.Length < 2) continue;
            if (!int.TryParse(parts[0], out var cuotaActual)) continue;
            if (!int.TryParse(parts[1], out var cuotaTotal))  continue;
            if (cuotaTotal <= 0) continue;

            // Col 2: concepto
            var concepto = reader.GetValue(2)?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(concepto)) continue;
            if (concepto.Equals("total", StringComparison.OrdinalIgnoreCase)) continue;

            // Col 1: fecha
            var startDate = ParseDate(reader.GetValue(1));
            if (startDate is null) continue;

            // Col 4: monto cuota
            var (montoCuota, moneda) = ParseAmount(reader.GetValue(4));

            // Col 5: saldo inicial
            var (saldoInicial, _) = ParseAmount(reader.GetValue(5));

            result.Add(new BacFinanciamientoRow(
                CardSuffix:          cardSuffix,
                Concepto:            concepto,
                StartDate:           startDate.Value,
                TermMonths:          cuotaTotal,
                CurrentInstallment:  cuotaActual,
                MontoCuota:          montoCuota,
                OriginalAmount:      saldoInicial,
                Moneda:              moneda));
        }

        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DateOnly? ParseDate(object? raw)
    {
        if (raw is null) return null;

        if (raw is DateTime dt) return DateOnly.FromDateTime(dt);

        if (raw is double d)
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(d)); }
            catch { /* fall through */ }
        }

        if (raw is string s)
        {
            s = s.Trim();
            if (DateOnly.TryParseExact(s, "dd/MM/yyyy", InvCulture, DateTimeStyles.None, out var date))
                return date;
            if (double.TryParse(s, NumberStyles.Any, InvCulture, out var d2))
            {
                try { return DateOnly.FromDateTime(DateTime.FromOADate(d2)); }
                catch { /* fall through */ }
            }
        }

        return null;
    }

    private static (decimal Amount, string Moneda) ParseAmount(object? raw)
    {
        if (raw is null)    return (0m, "CRC");
        if (raw is double d) return ((decimal)d, "CRC");
        if (raw is decimal dec) return (dec, "CRC");

        if (raw is string s)
        {
            var moneda  = s.Contains("USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "CRC";
            var cleaned = s.Replace("CRC", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("USD", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("\u00a0", "")  // NBSP como separador de miles
                           .Replace(" ", "")
                           .Replace(",", "")
                           .Trim();
            if (decimal.TryParse(cleaned, NumberStyles.Any, InvCulture, out var result))
                return (result, moneda);
        }

        return (0m, "CRC");
    }
}
