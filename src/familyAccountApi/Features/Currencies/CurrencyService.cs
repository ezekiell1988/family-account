using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Currencies.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Currencies;

public sealed class CurrencyService(AppDbContext db) : ICurrencyService
{
    public async Task<IReadOnlyList<CurrencyResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Currency
            .AsNoTracking()
            .OrderBy(c => c.CodeCurrency)
            .Select(c => new CurrencyResponse(
                c.IdCurrency,
                c.CodeCurrency,
                c.NameCurrency,
                c.SymbolCurrency))
            .ToListAsync(ct);
    }

    public async Task<CurrencyResponse?> GetByIdAsync(int idCurrency, CancellationToken ct = default)
    {
        return await db.Currency
            .AsNoTracking()
            .Where(c => c.IdCurrency == idCurrency)
            .Select(c => new CurrencyResponse(
                c.IdCurrency,
                c.CodeCurrency,
                c.NameCurrency,
                c.SymbolCurrency))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CurrencyResponse> CreateAsync(CreateCurrencyRequest request, CancellationToken ct = default)
    {
        var entity = new Currency
        {
            CodeCurrency = request.CodeCurrency,
            NameCurrency = request.NameCurrency,
            SymbolCurrency = request.SymbolCurrency
        };

        db.Currency.Add(entity);
        await db.SaveChangesAsync(ct);

        return new CurrencyResponse(
            entity.IdCurrency,
            entity.CodeCurrency,
            entity.NameCurrency,
            entity.SymbolCurrency);
    }

    public async Task<CurrencyResponse?> UpdateAsync(int idCurrency, UpdateCurrencyRequest request, CancellationToken ct = default)
    {
        var entity = await db.Currency.FindAsync([idCurrency], ct);
        if (entity is null) return null;

        entity.CodeCurrency = request.CodeCurrency;
        entity.NameCurrency = request.NameCurrency;
        entity.SymbolCurrency = request.SymbolCurrency;

        await db.SaveChangesAsync(ct);

        return new CurrencyResponse(
            entity.IdCurrency,
            entity.CodeCurrency,
            entity.NameCurrency,
            entity.SymbolCurrency);
    }

    public async Task<bool> DeleteAsync(int idCurrency, CancellationToken ct = default)
    {
        var deleted = await db.Currency
            .Where(c => c.IdCurrency == idCurrency)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}