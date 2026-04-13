using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.FinancialObligations.Parsers;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace FamilyAccountApi.BackgroundJobs;

/// <summary>
/// Entrada de un archivo XLS para el job de sincronización BAC Tasa Cero.
/// </summary>
public sealed record BacTcFileEntry(string CardSuffix, string RedisKey);

/// <summary>
/// Job de Hangfire que procesa los XLS de "Consulta de Financiamientos" de BAC Credomatic
/// (Tasa Cero) y upsertea <see cref="FinancialObligation"/> e
/// <see cref="FinancialObligationInstallment"/> en base de datos.
///
/// Flujo por cada archivo:
///   1. Lee bytes desde Redis (guardados por el endpoint de upload).
///   2. Parsea las filas del XLS con <see cref="FinancialObligationBacFinanciamientosParser"/>.
///   3. Por cada fila (= plan de financiamiento):
///        a. Resuelve cuentas contables, moneda y cuenta bancaria.
///        b. Crea la <see cref="FinancialObligation"/> si no existe (por MatchKeyword).
///        c. Reconstruye la tabla de amortización y hace upsert de cuotas.
///   4. Elimina los bytes de Redis.
/// </summary>
public sealed class FinancialObligationBacTcSyncJob(
    AppDbContext db,
    IDistributedCache cache,
    ILogger<FinancialObligationBacTcSyncJob> logger)
{
    /// <summary>Clave Redis donde se almacena cada archivo mientras espera ser procesado.</summary>
    public static string BuildRedisKey(string syncId, string cardSuffix)
        => $"bactcfinanciam:file:{syncId}:{cardSuffix}";

    // ── Mapeo fijo: "suffix-MONEDA" → idAccount del seed (AccountConfiguration.cs) ──
    // 143 = 2.1.01.08  BAC TC 5466-8608 Financiamientos Tasa Cero (₡)
    // 144 = 2.1.01.09  BAC TC 5466-8608 Financiamientos Tasa Cero ($)
    // 145 = 2.1.01.10  BAC TC 5491-6515 Financiamientos Tasa Cero (₡)
    //  28 = 2.1.01     BAC Credomatic – Tarjetas (agrupador) — fallback
    private static readonly IReadOnlyDictionary<string, int> AccountMap =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "8608-CRC", 143 },
            { "8608-USD", 144 },
            { "6515-CRC", 145 },
        };

    private const int FallbackAccountId = 28;
    private const int CurrencyCRC       = 1;
    private const int CurrencyUSD       = 2;

    // ─────────────────────────────────────────────────────────────────────────

    public async Task ProcessAsync(string syncId, List<BacTcFileEntry> entries)
    {
        foreach (var entry in entries)
        {
            // 1. Leer bytes desde Redis
            var bytes = await cache.GetAsync(entry.RedisKey);
            if (bytes is null || bytes.Length == 0)
            {
                logger.LogWarning(
                    "BacTcSyncJob: clave Redis {Key} no encontrada o expirada (syncId={SyncId}).",
                    entry.RedisKey, syncId);
                continue;
            }

            // 2. Parsear XLS
            IReadOnlyList<BacFinanciamientoRow> rows;
            try
            {
                using var stream = new MemoryStream(bytes);
                rows = new FinancialObligationBacFinanciamientosParser()
                           .Parse(stream, entry.CardSuffix);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "BacTcSyncJob: error al parsear XLS cardSuffix={Suffix} (syncId={SyncId}).",
                    entry.CardSuffix, syncId);
                continue;
            }

            logger.LogInformation(
                "BacTcSyncJob: {Count} planes parseados para tarjeta {Suffix} (syncId={SyncId}).",
                rows.Count, entry.CardSuffix, syncId);

            // 3. Procesar cada plan
            foreach (var row in rows)
            {
                try   { await UpsertPlanAsync(row); }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "BacTcSyncJob: error en plan [{Suffix}] {Concepto} (syncId={SyncId}).",
                        row.CardSuffix, row.Concepto, syncId);
                }
            }

            // 4. Limpiar Redis
            try   { await cache.RemoveAsync(entry.RedisKey); }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "BacTcSyncJob: no se pudo eliminar clave Redis '{Key}'.", entry.RedisKey);
            }
        }
    }

    // ── Lógica de upsert por plan ─────────────────────────────────────────────

    private async Task UpsertPlanAsync(BacFinanciamientoRow row)
    {
        // MatchKeyword: primeros 20 chars de concepto + sufijo tarjeta + año-mes
        var conceptoPart = row.Concepto.Length > 20
            ? row.Concepto[..20].Trim()
            : row.Concepto.Trim();
        var keyword = $"{conceptoPart}-{row.CardSuffix}-{row.StartDate:yyyy-MM}"
                          .ToUpperInvariant();

        // IDs de cuentas contables
        var accountKey = $"{row.CardSuffix}-{row.Moneda}";
        var idAccount  = AccountMap.TryGetValue(accountKey, out var acc)
                         ? acc
                         : FallbackAccountId;

        // Moneda
        var idCurrency = row.Moneda.Equals("USD", StringComparison.OrdinalIgnoreCase)
                         ? CurrencyUSD
                         : CurrencyCRC;

        // Cuenta bancaria de pago (opcional — puede no existir aún)
        var idBankAccPay = await db.BankAccount
            .Where(b => b.CodeBankAccount.Contains(row.CardSuffix) &&
                        b.CodeBankAccount.Contains(row.Moneda)     &&
                        b.IsActive)
            .Select(b => (int?)b.IdBankAccount)
            .FirstOrDefaultAsync();

        // Buscar obligación existente
        var obligation = await db.FinancialObligation
            .Include(o => o.FinancialObligationInstallments)
                .ThenInclude(i => i.FinancialObligationPayment)
            .FirstOrDefaultAsync(o => o.MatchKeyword == keyword);

        if (obligation is null)
        {
            var nameTrunc = row.Concepto.Length > 30
                ? row.Concepto[..30].Trim()
                : row.Concepto.Trim();

            obligation = new FinancialObligation
            {
                NameObligation       = $"BAC TC {row.CardSuffix} — {nameTrunc}",
                IdCurrency           = idCurrency,
                OriginalAmount       = row.OriginalAmount,
                InterestRate         = 0m,
                StartDate            = row.StartDate,
                TermMonths           = row.TermMonths,
                IdBankAccountPayment = idBankAccPay,
                IdAccountLongTerm    = idAccount,
                IdAccountShortTerm   = idAccount,
                IdAccountInterest    = idAccount,
                IdAccountLateFee     = null,
                IdAccountOther       = null,
                MatchKeyword         = keyword,
                StatusObligation     = "Activo",
                Notes                = "Tasa cero BAC. Cuotas reconstituidas desde XLS de Financiamientos."
            };
            db.FinancialObligation.Add(obligation);
            await db.SaveChangesAsync();

            logger.LogInformation(
                "BacTcSyncJob: creada obligación id={Id} keyword={Keyword}.",
                obligation.IdFinancialObligation, keyword);
        }
        else
        {
            logger.LogInformation(
                "BacTcSyncJob: obligación existente id={Id} keyword={Keyword}.",
                obligation.IdFinancialObligation, keyword);
        }

        // ── Upsert de cuotas ─────────────────────────────────────────────────
        //   Reconstruye la tabla de amortización: cuota plana (interés cero),
        //   fecha = startDate + n meses, balance = max(0, original - n * cuota).
        var now      = DateTime.UtcNow;
        int upserted = 0;

        for (int n = 1; n <= row.TermMonths; n++)
        {
            var dueDate = row.StartDate.AddMonths(n);
            var balance = Math.Max(0m, Math.Round(row.OriginalAmount - n * row.MontoCuota, 2));
            var status  = n < row.CurrentInstallment  ? "Pagada"
                        : n == row.CurrentInstallment ? "Vigente"
                        :                               "Pendiente";

            var inst = obligation.FinancialObligationInstallments
                .FirstOrDefault(i => i.NumberInstallment == n);

            if (inst is null)
            {
                inst = new FinancialObligationInstallment
                {
                    IdFinancialObligation = obligation.IdFinancialObligation,
                    NumberInstallment     = n
                };
                db.FinancialObligationInstallment.Add(inst);
            }

            inst.DueDate           = dueDate;
            inst.BalanceAfter      = balance;
            inst.AmountCapital     = row.MontoCuota;
            inst.AmountInterest    = 0m;
            inst.AmountLateFee     = 0m;
            inst.AmountOther       = 0m;
            inst.AmountTotal       = row.MontoCuota;
            inst.StatusInstallment = status;
            inst.SyncedAt          = now;
            upserted++;
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "BacTcSyncJob: {Count} cuotas upserted para obligación {Id}.",
            upserted, obligation.IdFinancialObligation);
    }
}
