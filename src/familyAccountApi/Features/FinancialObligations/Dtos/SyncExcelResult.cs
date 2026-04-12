namespace FamilyAccountApi.Features.FinancialObligations.Dtos;

/// <summary>
/// Resultado de sincronizar el Excel del banco contra el auxiliar de cuotas.
/// </summary>
public sealed record SyncExcelResult(
    int    InstallmentsUpserted,
    int    PaymentsCreated,
    int    PaymentsSkipped,              // Cuotas Pagadas sin movimiento BAC encontrado
    int?   ReclassificationEntryId,      // Asiento de reclasificación generado (si hubo cambio)
    decimal PreviousShortTermPortion,
    decimal NewShortTermPortion,
    IReadOnlyList<string> Warnings);     // Ej: "Cuota 3: 2 candidatos BAC, se usó el más cercano"
