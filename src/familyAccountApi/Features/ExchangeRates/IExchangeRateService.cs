using FamilyAccountApi.Features.ExchangeRates.Dtos;

namespace FamilyAccountApi.Features.ExchangeRates;

public interface IExchangeRateService
{
    Task<IReadOnlyList<ExchangeRateResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ExchangeRateResponse?> GetByIdAsync(int idExchangeRate, CancellationToken ct = default);
    Task<IReadOnlyList<ExchangeRateResponse>> GetByCurrencyAsync(int idCurrency, CancellationToken ct = default);
    Task<ExchangeRateResponse> CreateAsync(CreateExchangeRateRequest request, CancellationToken ct = default);
    Task<ExchangeRateResponse?> UpdateAsync(int idExchangeRate, UpdateExchangeRateRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idExchangeRate, CancellationToken ct = default);
}