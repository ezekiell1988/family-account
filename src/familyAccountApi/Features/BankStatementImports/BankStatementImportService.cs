using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankStatementImports.Dtos;
using FamilyAccountApi.Features.BankStatementImports.Parsers;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankStatementImports;

public sealed class BankStatementImportService(AppDbContext db) : IBankStatementImportService
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
        var bankAccountExists = await db.BankAccount
            .AsNoTracking()
            .AnyAsync(b => b.IdBankAccount == idBankAccount, ct);
        if (!bankAccountExists)
            throw new InvalidOperationException($"La cuenta bancaria con ID {idBankAccount} no existe.");

        var template = await db.BankStatementTemplate
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdBankStatementTemplate == idBankStatementTemplate, ct)
            ?? throw new InvalidOperationException($"La plantilla con ID {idBankStatementTemplate} no existe.");

        // Parsear el archivo HTML-XLS
        using var stream = file.OpenReadStream();
        var parsed = BcrXlsParser.Parse(
            stream,
            template.ColumnMappings,
            template.DateFormat,
            template.TimeFormat);

        var import = new BankStatementImport
        {
            IdBankAccount              = idBankAccount,
            IdBankStatementTemplate    = idBankStatementTemplate,
            FileName                   = file.FileName,
            ImportDate                 = DateTime.UtcNow,
            ImportedBy                 = importedBy,
            Status                     = "Completado",
            TotalTransactions          = parsed.Count,
            ProcessedTransactions      = parsed.Count,
            ErrorMessage               = null
        };

        db.BankStatementImport.Add(import);
        await db.SaveChangesAsync(ct);

        var transactions = parsed.Select(p => new BankStatementTransaction
        {
            IdBankStatementImport = import.IdBankStatementImport,
            AccountingDate        = p.AccountingDate,
            TransactionDate       = p.TransactionDate,
            TransactionTime       = p.TransactionTime,
            DocumentNumber        = p.DocumentNumber,
            Description           = p.Description,
            DebitAmount           = p.DebitAmount,
            CreditAmount          = p.CreditAmount,
            Balance               = p.Balance,
            IsReconciled          = false
        }).ToList();

        db.BankStatementTransaction.AddRange(transactions);
        await db.SaveChangesAsync(ct);

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
}
