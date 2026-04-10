using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankStatementImports.Parsers;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace FamilyAccountApi.BackgroundJobs;

/// <summary>
/// Job de Hangfire que ejecuta el ciclo completo de importación de un extracto bancario:
///   1. Recupera el archivo (bytes) guardado en Redis por el endpoint de upload.
///   2. Marca la importación como "Procesando".
///   3. Selecciona el parser adecuado vía <see cref="BankStatementParserFactory"/>.
///   4. Auto-clasifica cada transacción con <see cref="KeywordClassifier"/>.
///   5. Persiste las <see cref="BankStatementTransaction"/> en base de datos.
///   6. Actualiza el registro de importación a "Completado" (o "Error").
///   7. Elimina el archivo de Redis.
/// </summary>
public sealed class BankStatementImportJob(
    AppDbContext db,
    IDistributedCache cache,
    ILogger<BankStatementImportJob> logger,
    BankStatementParserFactory parserFactory)
{
    /// <summary>Clave Redis donde se almacena el archivo mientras espera ser procesado.</summary>
    public static string BuildRedisKey(int importId) => $"bankstatement:upload:{importId}";

    public async Task ProcessAsync(int importId)
    {
        // ── 0. Cargar el registro de importación ──────────────────────────────
        var import = await db.BankStatementImport
            .Include(i => i.IdBankStatementTemplateNavigation)
            .FirstOrDefaultAsync(i => i.IdBankStatementImport == importId);

        if (import is null)
        {
            logger.LogWarning(
                "BankStatementImportJob: importId={ImportId} no encontrado en BD.", importId);
            return;
        }

        // Idempotencia: si Hangfire re-ejecuta un job ya completado, salir sin hacer nada
        if (import.Status == "Completado")
        {
            await TryRemoveCacheAsync(BuildRedisKey(importId));
            return;
        }

        // ── 1. Marcar como "Procesando" ───────────────────────────────────────
        import.Status = "Procesando";
        await db.SaveChangesAsync();

        var redisKey = BuildRedisKey(importId);

        try
        {
            // ── 2. Recuperar el archivo desde Redis ───────────────────────────
            var fileBytes = await cache.GetAsync(redisKey);

            if (fileBytes is null || fileBytes.Length == 0)
            {
                import.Status       = "Error";
                import.ErrorMessage = "Archivo temporal expirado en caché. Vuelva a cargar el extracto.";
                await db.SaveChangesAsync();
                logger.LogError(
                    "BankStatementImportJob: clave Redis expirada para importId={ImportId}.", importId);
                return; // sin re-throw: el archivo ya no existe, reintentar no sirve
            }

            var template = import.IdBankStatementTemplateNavigation!;

            // ── 3. Eliminar transacciones de intentos anteriores (idempotencia) ─
            await db.BankStatementTransaction
                .Where(t => t.IdBankStatementImport == importId)
                .ExecuteDeleteAsync();

            // ── 4. Parsear el archivo según el formato del template ───────────────
            using var stream = new MemoryStream(fileBytes);
            var parser = parserFactory.GetParser(template.CodeTemplate);
            var parsed = parser.Parse(
                stream,
                template.ColumnMappings,
                template.DateFormat,
                template.TimeFormat);

            // ── 5. Auto-clasificar y construir entidades ──────────────────────
            var transactions = parsed
                .Select(p =>
                {
                    var cls = KeywordClassifier.Classify(p.Description, template.KeywordRules);
                    return new BankStatementTransaction
                    {
                        IdBankStatementImport = importId,
                        AccountingDate        = p.AccountingDate,
                        TransactionDate       = p.TransactionDate,
                        TransactionTime       = p.TransactionTime,
                        DocumentNumber        = p.DocumentNumber,
                        Description           = p.Description,
                        DebitAmount           = p.DebitAmount,
                        CreditAmount          = p.CreditAmount,
                        Balance               = p.Balance,
                        IsReconciled          = false,
                        IdBankMovementType    = cls?.IdBankMovementType,
                        IdAccountCounterpart  = cls?.IdAccountCounterpart
                    };
                })
                .ToList();

            db.BankStatementTransaction.AddRange(transactions);

            // ── 6. Actualizar registro de importación ─────────────────────────
            import.TotalTransactions     = transactions.Count;
            import.ProcessedTransactions = transactions.Count;
            import.Status                = "Completado";
            import.ErrorMessage          = null;

            await db.SaveChangesAsync();

            // ── 7. Limpiar Redis ───────────────────────────────────────────────
            await TryRemoveCacheAsync(redisKey);

            logger.LogInformation(
                "BankStatementImportJob completado: importId={ImportId}, {Count} transacciones guardadas.",
                importId, transactions.Count);
        }
        catch (Exception ex)
        {
            import.Status       = "Error";
            import.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            try { await db.SaveChangesAsync(); } catch { /* best effort */ }

            logger.LogError(ex,
                "BankStatementImportJob: error procesando importId={ImportId}.", importId);

            // Re-lanzar para que Hangfire aplique la política de reintentos configurada
            throw;
        }
    }

    private async Task TryRemoveCacheAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "BankStatementImportJob: no se pudo eliminar clave Redis '{Key}'.", key);
        }
    }
}
