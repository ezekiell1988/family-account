using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ExchangeRates.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ExchangeRates;

public sealed class ExchangeRateService(AppDbContext db) : IExchangeRateService
{
    public async Task<IReadOnlyList<ExchangeRateResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.ExchangeRate
            .AsNoTracking()
            .Include(er => er.IdCurrencyNavigation)
            .OrderByDescending(er => er.RateDate)
            .ThenBy(er => er.IdCurrencyNavigation.CodeCurrency)
            .Select(er => new ExchangeRateResponse(
                er.IdExchangeRate,
                er.IdCurrency,
                er.IdCurrencyNavigation.CodeCurrency,
                er.IdCurrencyNavigation.NameCurrency,
                er.RateDate,
                er.RateValue))
            .ToListAsync(ct);
    }

    public async Task<ExchangeRateResponse?> GetByIdAsync(int idExchangeRate, CancellationToken ct = default)
    {
        return await db.ExchangeRate
            .AsNoTracking()
            .Include(er => er.IdCurrencyNavigation)
            .Where(er => er.IdExchangeRate == idExchangeRate)
            .Select(er => new ExchangeRateResponse(
                er.IdExchangeRate,
                er.IdCurrency,
                er.IdCurrencyNavigation.CodeCurrency,
                er.IdCurrencyNavigation.NameCurrency,
                er.RateDate,
                er.RateValue))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ExchangeRateResponse>> GetByCurrencyAsync(int idCurrency, CancellationToken ct = default)
    {
        return await db.ExchangeRate
            .AsNoTracking()
            .Include(er => er.IdCurrencyNavigation)
            .Where(er => er.IdCurrency == idCurrency)
            .OrderByDescending(er => er.RateDate)
            .Select(er => new ExchangeRateResponse(
                er.IdExchangeRate,
                er.IdCurrency,
                er.IdCurrencyNavigation.CodeCurrency,
                er.IdCurrencyNavigation.NameCurrency,
                er.RateDate,
                er.RateValue))
            .ToListAsync(ct);
    }

    public async Task<ExchangeRateResponse> CreateAsync(CreateExchangeRateRequest request, CancellationToken ct = default)
    {
        var currencyExists = await db.Currency
            .AnyAsync(c => c.IdCurrency == request.IdCurrency, ct);

        if (!currencyExists)
            throw new InvalidOperationException("La moneda indicada no existe.");

        var entity = new ExchangeRate
        {
            IdCurrency = request.IdCurrency,
            RateDate = request.RateDate,
            RateValue = request.RateValue
        };

        db.ExchangeRate.Add(entity);
        await db.SaveChangesAsync(ct);

        return await db.ExchangeRate
            .AsNoTracking()
            .Include(er => er.IdCurrencyNavigation)
            .Where(er => er.IdExchangeRate == entity.IdExchangeRate)
            .Select(er => new ExchangeRateResponse(
                er.IdExchangeRate,
                er.IdCurrency,
                er.IdCurrencyNavigation.CodeCurrency,
                er.IdCurrencyNavigation.NameCurrency,
                er.RateDate,
                er.RateValue))
            .FirstAsync(ct);
    }

    public async Task<ExchangeRateResponse?> UpdateAsync(int idExchangeRate, UpdateExchangeRateRequest request, CancellationToken ct = default)
    {
        var entity = await db.ExchangeRate.FindAsync([idExchangeRate], ct);
        if (entity is null) return null;

        var currencyExists = await db.Currency
            .AnyAsync(c => c.IdCurrency == request.IdCurrency, ct);

        if (!currencyExists)
            throw new InvalidOperationException("La moneda indicada no existe.");

        entity.IdCurrency = request.IdCurrency;
        entity.RateDate = request.RateDate;
        entity.RateValue = request.RateValue;

        await db.SaveChangesAsync(ct);

        return await db.ExchangeRate
            .AsNoTracking()
            .Include(er => er.IdCurrencyNavigation)
            .Where(er => er.IdExchangeRate == entity.IdExchangeRate)
            .Select(er => new ExchangeRateResponse(
                er.IdExchangeRate,
                er.IdCurrency,
                er.IdCurrencyNavigation.CodeCurrency,
                er.IdCurrencyNavigation.NameCurrency,
                er.RateDate,
                er.RateValue))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> DeleteAsync(int idExchangeRate, CancellationToken ct = default)
    {
        var deleted = await db.ExchangeRate
            .Where(er => er.IdExchangeRate == idExchangeRate)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}