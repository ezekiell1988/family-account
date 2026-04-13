namespace FamilyAccountApi.Features.FinancialObligations.Parsers;

/// <summary>
/// Una fila de datos del XLS de "Consulta de Financiamientos" de BAC Credomatic.
/// Cada fila corresponde a un plan de financiamiento (una obligación Tasa Cero).
/// </summary>
public sealed record BacFinanciamientoRow(
    string   CardSuffix,         // Ej: "8608" | "6515" — del nombre del archivo
    string   Concepto,           // Descripción del financiamiento (col 2)
    DateOnly StartDate,          // Fecha de primer vencimiento (col 1)
    int      TermMonths,         // Total de cuotas  — de "009/012" → 12
    int      CurrentInstallment, // Cuota actual     — de "009/012" →  9
    decimal  MontoCuota,         // Monto por cuota (col 4)
    decimal  OriginalAmount,     // Saldo inicial    (col 5)
    string   Moneda);            // "CRC" | "USD" — detectado del monto
