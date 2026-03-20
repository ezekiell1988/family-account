using FamilyAccountApi.Features.CostCenters.Dtos;

namespace FamilyAccountApi.Features.CostCenters;

public interface ICostCenterService
{
    Task<IReadOnlyList<CostCenterResponse>> GetAllAsync(CancellationToken ct = default);
    Task<CostCenterResponse?> GetByIdAsync(int idCostCenter, CancellationToken ct = default);
    Task<CostCenterResponse> CreateAsync(CreateCostCenterRequest request, CancellationToken ct = default);
    Task<CostCenterResponse?> UpdateAsync(int idCostCenter, UpdateCostCenterRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idCostCenter, CancellationToken ct = default);
}
