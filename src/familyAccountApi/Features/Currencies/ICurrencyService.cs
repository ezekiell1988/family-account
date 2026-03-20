using FamilyAccountApi.Features.Currencies.Dtos;

namespace FamilyAccountApi.Features.Currencies;

public interface ICurrencyService
{
    Task<IReadOnlyList<CurrencyResponse>> GetAllAsync(CancellationToken ct = default);
    Task<CurrencyResponse?> GetByIdAsync(int idCurrency, CancellationToken ct = default);
    Task<CurrencyResponse> CreateAsync(CreateCurrencyRequest request, CancellationToken ct = default);
    Task<CurrencyResponse?> UpdateAsync(int idCurrency, UpdateCurrencyRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idCurrency, CancellationToken ct = default);
}