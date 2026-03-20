using FamilyAccountApi.Features.FiscalPeriods.Dtos;

namespace FamilyAccountApi.Features.FiscalPeriods;

public interface IFiscalPeriodService
{
    Task<IReadOnlyList<FiscalPeriodResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FiscalPeriodResponse>> GetByYearAsync(int year, CancellationToken ct = default);
    Task<FiscalPeriodResponse?> GetByIdAsync(int idFiscalPeriod, CancellationToken ct = default);
    Task<FiscalPeriodResponse> CreateAsync(CreateFiscalPeriodRequest request, CancellationToken ct = default);
    Task<FiscalPeriodResponse?> UpdateAsync(int idFiscalPeriod, UpdateFiscalPeriodRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idFiscalPeriod, CancellationToken ct = default);
}
