using FamilyAccountApi.Features.FinancialObligations.Dtos;

namespace FamilyAccountApi.Features.FinancialObligations;

public interface IFinancialObligationService
{
    Task<IReadOnlyList<FinancialObligationResponse>> GetAllAsync(CancellationToken ct = default);
    Task<FinancialObligationResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<FinancialObligationSummaryResponse?> GetSummaryAsync(int id, CancellationToken ct = default);
    Task<FinancialObligationResponse> CreateAsync(CreateFinancialObligationRequest request, CancellationToken ct = default);
    Task<FinancialObligationResponse?> UpdateAsync(int id, UpdateFinancialObligationRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<SyncExcelResult> SyncExcelAsync(int id, Stream fileStream, CancellationToken ct = default);
    Task<FinancialObligationPaymentResponse> RegisterPaymentAsync(int installmentId, RegisterPaymentRequest request, CancellationToken ct = default);
    Task<int?> ReclassifyAsync(int id, DateOnly asOfDate, CancellationToken ct = default);
}
