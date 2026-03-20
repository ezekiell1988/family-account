using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankStatementTemplates.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankStatementTemplates;

public sealed class BankStatementTemplateService(AppDbContext db) : IBankStatementTemplateService
{
    public async Task<IReadOnlyList<BankStatementTemplateResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.BankStatementTemplate
            .AsNoTracking()
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<BankStatementTemplateResponse?> GetByIdAsync(int idBankStatementTemplate, CancellationToken ct = default)
    {
        return await db.BankStatementTemplate
            .AsNoTracking()
            .Where(b => b.IdBankStatementTemplate == idBankStatementTemplate)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BankStatementTemplateResponse> CreateAsync(CreateBankStatementTemplateRequest request, CancellationToken ct = default)
    {
        var entity = new BankStatementTemplate
        {
            CodeTemplate = request.CodeTemplate,
            NameTemplate = request.NameTemplate,
            BankName = request.BankName,
            ColumnMappings = request.ColumnMappings,
            KeywordRules = request.KeywordRules,
            DateFormat = request.DateFormat,
            TimeFormat = request.TimeFormat,
            IsActive = request.IsActive,
            Notes = request.Notes
        };

        db.BankStatementTemplate.Add(entity);
        await db.SaveChangesAsync(ct);

        return new BankStatementTemplateResponse(
            entity.IdBankStatementTemplate,
            entity.CodeTemplate,
            entity.NameTemplate,
            entity.BankName,
            entity.ColumnMappings,
            entity.KeywordRules,
            entity.DateFormat,
            entity.TimeFormat,
            entity.IsActive,
            entity.Notes);
    }

    public async Task<BankStatementTemplateResponse?> UpdateAsync(int idBankStatementTemplate, UpdateBankStatementTemplateRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankStatementTemplate.FindAsync([idBankStatementTemplate], ct);
        if (entity is null) return null;

        entity.CodeTemplate = request.CodeTemplate;
        entity.NameTemplate = request.NameTemplate;
        entity.BankName = request.BankName;
        entity.ColumnMappings = request.ColumnMappings;
        entity.KeywordRules = request.KeywordRules;
        entity.DateFormat = request.DateFormat;
        entity.TimeFormat = request.TimeFormat;
        entity.IsActive = request.IsActive;
        entity.Notes = request.Notes;

        await db.SaveChangesAsync(ct);

        return new BankStatementTemplateResponse(
            entity.IdBankStatementTemplate,
            entity.CodeTemplate,
            entity.NameTemplate,
            entity.BankName,
            entity.ColumnMappings,
            entity.KeywordRules,
            entity.DateFormat,
            entity.TimeFormat,
            entity.IsActive,
            entity.Notes);
    }

    public async Task<bool> DeleteAsync(int idBankStatementTemplate, CancellationToken ct = default)
    {
        var deleted = await db.BankStatementTemplate
            .Where(b => b.IdBankStatementTemplate == idBankStatementTemplate)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    private static System.Linq.Expressions.Expression<Func<BankStatementTemplate, BankStatementTemplateResponse>> MapResponse()
    {
        return b => new BankStatementTemplateResponse(
            b.IdBankStatementTemplate,
            b.CodeTemplate,
            b.NameTemplate,
            b.BankName,
            b.ColumnMappings,
            b.KeywordRules,
            b.DateFormat,
            b.TimeFormat,
            b.IsActive,
            b.Notes);
    }
}
