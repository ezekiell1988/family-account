using ExcelDataReader;
using System.Globalization;

namespace FamilyAccountApi.Features.FinancialObligations.Parsers;

/// <summary>
/// Parsea el Excel XLSX de tabla de pagos exportado por COOPEALIANZA.
///
/// Estructura esperada (fila 1 = cabecera):
///   A: Número de transacción  B: Fecha de vencimiento  C: Saldo
///   D: Capital  E: Interés  F: Mora  G: Otros  H: Total  I: Estado
///
/// Los montos vienen como strings con símbolo ₡ y separadores de miles con NBSP (U+00A0).
/// Las fechas vienen como número serial OADate de Excel.
/// </summary>
public sealed class FinancialObligationExcelParser
{
    private static readonly CultureInfo InvCulture = CultureInfo.InvariantCulture;

    public IReadOnlyList<InstallmentRow> Parse(Stream stream)
    {
        using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        var result = new List<InstallmentRow>();

        // Saltar la fila de cabecera
        if (!reader.Read()) return result;

        while (reader.Read())
        {
            // Columna A: número de cuota (puede ser int o string)
            var rawNumber = reader.GetValue(0);
            if (rawNumber is null) continue;
            if (!int.TryParse(rawNumber.ToString(), out var number)) continue;

            // Columna B: fecha de vencimiento (serial OADate o string dd/MM/yyyy)
            var dueDate = ParseDate(reader.GetValue(1));
            if (dueDate is null) continue;

            // Columnas C-H: montos
            var balanceAfter    = ParseAmount(reader.GetValue(2));
            var amountCapital   = ParseAmount(reader.GetValue(3));
            var amountInterest  = ParseAmount(reader.GetValue(4));
            var amountLateFee   = ParseAmount(reader.GetValue(5));
            var amountOther     = ParseAmount(reader.GetValue(6));
            var amountTotal     = ParseAmount(reader.GetValue(7));

            // Columna I: estado
            var rawStatus = reader.GetValue(8)?.ToString()?.Trim() ?? "Pendiente";
            var status    = NormalizeStatus(rawStatus);

            result.Add(new InstallmentRow(
                NumberInstallment: number,
                DueDate:           dueDate.Value,
                BalanceAfter:      balanceAfter,
                AmountCapital:     amountCapital,
                AmountInterest:    amountInterest,
                AmountLateFee:     amountLateFee,
                AmountOther:       amountOther,
                AmountTotal:       amountTotal > 0 ? amountTotal : amountCapital + amountInterest + amountLateFee + amountOther,
                StatusInstallment: status));
        }

        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DateOnly? ParseDate(object? raw)
    {
        if (raw is null) return null;

        // ExcelDataReader puede entregar DateTime directamente para celdas de fecha
        if (raw is DateTime dt) return DateOnly.FromDateTime(dt);

        // Serial OADate (double)
        if (raw is double d || (raw is string ds && double.TryParse(ds, NumberStyles.Any, InvCulture, out d)))
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(d)); }
            catch { /* sigue */ }
        }

        // Texto dd/MM/yyyy
        if (raw is string s)
        {
            if (DateOnly.TryParseExact(s.Trim(), "dd/MM/yyyy", InvCulture, DateTimeStyles.None, out var date))
                return date;
        }

        return null;
    }

    private static decimal ParseAmount(object? raw)
    {
        if (raw is null) return 0m;

        // Número directo (double)
        if (raw is double d) return (decimal)d;
        if (raw is decimal dec) return dec;

        // String con símbolo ₡ y NBSP como separador de miles
        var s = raw.ToString()!
            .Replace("₡", "")
            .Replace("\u00A0", "")   // NBSP
            .Replace("\u202F", "")   // Narrow NBSP
            .Replace(" ", "")
            .Replace(",", ".")       // separador decimal latinoamérica
            .Trim();

        // Si tiene más de un punto, el último es decimal
        var parts = s.Split('.');
        if (parts.Length > 2)
            s = string.Join("", parts[..^1]) + "." + parts[^1];

        return decimal.TryParse(s, NumberStyles.Any, InvCulture, out var result) ? result : 0m;
    }

    private static string NormalizeStatus(string raw) => raw.ToLowerInvariant() switch
    {
        "pagada"   => "Pagada",
        "vigente"  => "Vigente",
        "vencida"  => "Vencida",
        _          => "Pendiente"
    };
}

public sealed record InstallmentRow(
    int      NumberInstallment,
    DateOnly DueDate,
    decimal  BalanceAfter,
    decimal  AmountCapital,
    decimal  AmountInterest,
    decimal  AmountLateFee,
    decimal  AmountOther,
    decimal  AmountTotal,
    string   StatusInstallment);
