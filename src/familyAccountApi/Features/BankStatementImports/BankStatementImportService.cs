using FamilyAccountApi.BackgroundJobs;
using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankStatementImports.Dtos;
using FamilyAccountApi.Features.BankStatementImports.Parsers;
using FamilyAccountApi.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FamilyAccountApi.Features.BankStatementImports;

public sealed class BankStatementImportService(
    AppDbContext db,
    IDistributedCache cache,
    IBackgroundJobClient backgroundJobs) : IBankStatementImportService
{
    public async Task<IReadOnlyList<BankStatementImportResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.BankStatementImport
            .AsNoTracking()
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<BankStatementImportResponse?> GetByIdAsync(int idBankStatementImport, CancellationToken ct = default)
    {
        return await db.BankStatementImport
            .AsNoTracking()
            .Where(b => b.IdBankStatementImport == idBankStatementImport)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BankStatementImportResponse> CreateAsync(CreateBankStatementImportRequest request, int importedBy, CancellationToken ct = default)
    {
        // Validar que la cuenta bancaria existe
        var bankAccountExists = await db.BankAccount
            .AsNoTracking()
            .AnyAsync(b => b.IdBankAccount == request.IdBankAccount, ct);

        if (!bankAccountExists)
            throw new InvalidOperationException($"La cuenta bancaria con ID {request.IdBankAccount} no existe.");

        // Validar que la plantilla existe
        var templateExists = await db.BankStatementTemplate
            .AsNoTracking()
            .AnyAsync(t => t.IdBankStatementTemplate == request.IdBankStatementTemplate, ct);

        if (!templateExists)
            throw new InvalidOperationException($"La plantilla con ID {request.IdBankStatementTemplate} no existe.");

        var entity = new BankStatementImport
        {
            IdBankAccount = request.IdBankAccount,
            IdBankStatementTemplate = request.IdBankStatementTemplate,
            FileName = request.FileName,
            ImportDate = DateTime.UtcNow,
            ImportedBy = importedBy,
            Status = request.Status,
            TotalTransactions = request.TotalTransactions,
            ProcessedTransactions = 0,
            ErrorMessage = request.ErrorMessage
        };

        db.BankStatementImport.Add(entity);
        await db.SaveChangesAsync(ct);

        // Cargar navegaciones para el response
        await db.Entry(entity)
            .Reference(e => e.IdBankAccountNavigation)
            .Query()
            .Include(b => b.IdAccountNavigation)
            .LoadAsync(ct);

        await db.Entry(entity)
            .Reference(e => e.IdBankStatementTemplateNavigation)
            .LoadAsync(ct);

        await db.Entry(entity)
            .Reference(e => e.ImportedByNavigation)
            .LoadAsync(ct);

        return new BankStatementImportResponse(
            entity.IdBankStatementImport,
            entity.IdBankAccount,
            entity.IdBankAccountNavigation.CodeBankAccount,
            entity.IdBankAccountNavigation.IdAccountNavigation.NameAccount,
            entity.IdBankStatementTemplate,
            entity.IdBankStatementTemplateNavigation.CodeTemplate,
            entity.IdBankStatementTemplateNavigation.NameTemplate,
            entity.FileName,
            entity.ImportDate,
            entity.ImportedBy,
            entity.ImportedByNavigation.NameUser,
            entity.Status,
            entity.TotalTransactions,
            entity.ProcessedTransactions,
            entity.ErrorMessage);
    }

    public async Task<BankStatementImportResponse?> UpdateAsync(int idBankStatementImport, UpdateBankStatementImportRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankStatementImport.FindAsync([idBankStatementImport], ct);
        if (entity is null) return null;

        entity.Status = request.Status;
        entity.TotalTransactions = request.TotalTransactions;
        entity.ProcessedTransactions = request.ProcessedTransactions;
        entity.ErrorMessage = request.ErrorMessage;

        await db.SaveChangesAsync(ct);

        // Cargar navegaciones para el response
        await db.Entry(entity)
            .Reference(e => e.IdBankAccountNavigation)
            .Query()
            .Include(b => b.IdAccountNavigation)
            .LoadAsync(ct);

        await db.Entry(entity)
            .Reference(e => e.IdBankStatementTemplateNavigation)
            .LoadAsync(ct);

        await db.Entry(entity)
            .Reference(e => e.ImportedByNavigation)
            .LoadAsync(ct);

        return new BankStatementImportResponse(
            entity.IdBankStatementImport,
            entity.IdBankAccount,
            entity.IdBankAccountNavigation.CodeBankAccount,
            entity.IdBankAccountNavigation.IdAccountNavigation.NameAccount,
            entity.IdBankStatementTemplate,
            entity.IdBankStatementTemplateNavigation.CodeTemplate,
            entity.IdBankStatementTemplateNavigation.NameTemplate,
            entity.FileName,
            entity.ImportDate,
            entity.ImportedBy,
            entity.ImportedByNavigation.NameUser,
            entity.Status,
            entity.TotalTransactions,
            entity.ProcessedTransactions,
            entity.ErrorMessage);
    }

    public async Task<bool> DeleteAsync(int idBankStatementImport, CancellationToken ct = default)
    {
        var deleted = await db.BankStatementImport
            .Where(b => b.IdBankStatementImport == idBankStatementImport)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    private static System.Linq.Expressions.Expression<Func<BankStatementImport, BankStatementImportResponse>> MapResponse()
    {
        return b => new BankStatementImportResponse(
            b.IdBankStatementImport,
            b.IdBankAccount,
            b.IdBankAccountNavigation.CodeBankAccount,
            b.IdBankAccountNavigation.IdAccountNavigation.NameAccount,
            b.IdBankStatementTemplate,
            b.IdBankStatementTemplateNavigation.CodeTemplate,
            b.IdBankStatementTemplateNavigation.NameTemplate,
            b.FileName,
            b.ImportDate,
            b.ImportedBy,
            b.ImportedByNavigation.NameUser,
            b.Status,
            b.TotalTransactions,
            b.ProcessedTransactions,
            b.ErrorMessage);
    }

    public async Task<BankStatementImportResponse> UploadAsync(
        IFormFile file,
        int       idBankAccount,
        int       idBankStatementTemplate,
        int       importedBy,
        CancellationToken ct = default)
    {
        // ── Validaciones ──────────────────────────────────────────────────
        var bankAccountExists = await db.BankAccount
            .AsNoTracking()
            .AnyAsync(b => b.IdBankAccount == idBankAccount, ct);
        if (!bankAccountExists)
            throw new InvalidOperationException($"La cuenta bancaria con ID {idBankAccount} no existe.");

        var templateExists = await db.BankStatementTemplate
            .AsNoTracking()
            .AnyAsync(t => t.IdBankStatementTemplate == idBankStatementTemplate, ct);
        if (!templateExists)
            throw new InvalidOperationException($"La plantilla con ID {idBankStatementTemplate} no existe.");

        // ── Leer bytes del archivo (el stream solo vive mientras dura el request) ─
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var fileBytes = ms.ToArray();

        // ── Crear registro en BD con estado Pendiente ─────────────────────
        var import = new BankStatementImport
        {
            IdBankAccount           = idBankAccount,
            IdBankStatementTemplate = idBankStatementTemplate,
            FileName                = file.FileName,
            ImportDate              = DateTime.UtcNow,
            ImportedBy              = importedBy,
            Status                  = "Pendiente",
            TotalTransactions       = 0,
            ProcessedTransactions   = 0,
            ErrorMessage            = null
        };

        db.BankStatementImport.Add(import);
        await db.SaveChangesAsync(ct);

        // ── Guardar archivo en Redis (TTL 1 h, margen para reintentos del job) ─
        var redisKey = BankStatementImportJob.BuildRedisKey(import.IdBankStatementImport);
        await cache.SetAsync(redisKey, fileBytes,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }, ct);

        // ── Encolar el job de Hangfire ─────────────────────────────────────
        backgroundJobs.Enqueue<BankStatementImportJob>(
            job => job.ProcessAsync(import.IdBankStatementImport));

        // Cargar navegaciones para el response
        await db.Entry(import)
            .Reference(e => e.IdBankAccountNavigation)
            .Query()
            .Include(b => b.IdAccountNavigation)
            .LoadAsync(ct);

        await db.Entry(import)
            .Reference(e => e.IdBankStatementTemplateNavigation)
            .LoadAsync(ct);

        await db.Entry(import)
            .Reference(e => e.ImportedByNavigation)
            .LoadAsync(ct);

        return new BankStatementImportResponse(
            import.IdBankStatementImport,
            import.IdBankAccount,
            import.IdBankAccountNavigation.CodeBankAccount,
            import.IdBankAccountNavigation.IdAccountNavigation.NameAccount,
            import.IdBankStatementTemplate,
            import.IdBankStatementTemplateNavigation.CodeTemplate,
            import.IdBankStatementTemplateNavigation.NameTemplate,
            import.FileName,
            import.ImportDate,
            import.ImportedBy,
            import.ImportedByNavigation.NameUser,
            import.Status,
            import.TotalTransactions,
            import.ProcessedTransactions,
            import.ErrorMessage);
    }

    public async Task<BulkClassifyResult> ClassifyBatchAsync(
        int                 importId,
        BulkClassifyRequest request,
        CancellationToken   ct = default)
    {
        // ── 1. Cargar el import + template ────────────────────────────────
        var import = await db.BankStatementImport
            .Include(i => i.IdBankStatementTemplateNavigation)
            .FirstOrDefaultAsync(i => i.IdBankStatementImport == importId, ct)
            ?? throw new InvalidOperationException($"El import {importId} no existe.");

        var template = import.IdBankStatementTemplateNavigation!;

        // ── 2. Cargar las transacciones involucradas ───────────────────────
        var txIds = request.Items.Select(x => x.IdBankStatementTransaction).ToList();
        var transactions = await db.BankStatementTransaction
            .Where(t => txIds.Contains(t.IdBankStatementTransaction))
            .ToListAsync(ct);

        var txMap = transactions.ToDictionary(t => t.IdBankStatementTransaction);

        // ── 3. Clasificar ─────────────────────────────────────────────────
        int classified = 0;
        var newKeywordRules = new List<KeywordRule>();

        foreach (var item in request.Items)
        {
            if (!txMap.TryGetValue(item.IdBankStatementTransaction, out var tx)) continue;

            // No re-clasificar transacciones que ya tienen asiento contable generado
            if (tx.IdAccountingEntry is not null) continue;

            tx.IdBankMovementType   = item.IdBankMovementType;
            tx.IdAccountCounterpart = item.IdAccountCounterpart;
            tx.IdCostCenter         = item.IdCostCenter;
            classified++;

            // ── 4. Aprender keyword si se solicita ─────────────────────────
            if (item.LearnKeyword)
            {
                var desc = !string.IsNullOrWhiteSpace(item.KeywordText)
                    ? item.KeywordText.Trim()
                    : tx.Description?.Trim();
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    // Solo agregar si no está ya cubierto por un keyword existente
                    var existingResult = KeywordClassifier.Classify(desc, template.KeywordRules);
                    if (existingResult is null)
                    {
                        newKeywordRules.Add(new KeywordRule
                        {
                            Keywords            = [desc],
                            IdBankMovementType  = item.IdBankMovementType,
                            IdAccountCounterpart = item.IdAccountCounterpart,
                            MatchMode           = "Any"
                        });
                    }
                }
            }
        }

        // ── 5. Actualizar template con nuevos keywords ────────────────────
        int keywordsAdded = 0;
        if (newKeywordRules.Count > 0)
        {
            var opts   = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var existing = string.IsNullOrWhiteSpace(template.KeywordRules)
                ? []
                : JsonSerializer.Deserialize<List<KeywordRule>>(template.KeywordRules, opts) ?? [];

            existing.AddRange(newKeywordRules);
            template.KeywordRules = JsonSerializer.Serialize(existing, opts);
            keywordsAdded = newKeywordRules.Count;
        }

        await db.SaveChangesAsync(ct);
        return new BulkClassifyResult(classified, keywordsAdded);
    }
}
