namespace FamilyAccountApi.Features.FinancialObligations.Dtos;

/// <summary>
/// Respuesta del endpoint POST /financial-obligations/sync-bac-financiamientos.
/// El procesamiento real ocurre en el job de Hangfire; este response confirma que
/// los archivos fueron recibidos y el job fue encolado.
/// </summary>
public sealed record SyncBacFinanciamientosResponse(
    string SyncId,        // GUID de la sesión de sincronización
    string JobId,         // ID del job de Hangfire devuelto por Enqueue<>
    int    FilesSubmitted, // Cantidad de archivos XLS recibidos
    string Status);       // "Enqueued"
