using FamilyAccountApi.Features.InventoryAdjustmentTypes.Dtos;

namespace FamilyAccountApi.Features.InventoryAdjustmentTypes;

public interface IInventoryAdjustmentTypeService
{
    Task<IReadOnlyList<InventoryAdjustmentTypeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InventoryAdjustmentTypeResponse>> GetActiveAsync(CancellationToken ct = default);
    Task<InventoryAdjustmentTypeResponse?> GetByIdAsync(int idInventoryAdjustmentType, CancellationToken ct = default);
    Task<InventoryAdjustmentTypeResponse> CreateAsync(CreateInventoryAdjustmentTypeRequest request, CancellationToken ct = default);
    Task<InventoryAdjustmentTypeResponse?> UpdateAsync(int idInventoryAdjustmentType, UpdateInventoryAdjustmentTypeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idInventoryAdjustmentType, CancellationToken ct = default);
}
