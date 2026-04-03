using FamilyAccountApi.Features.InventoryAdjustments.Dtos;

namespace FamilyAccountApi.Features.InventoryAdjustments;

public interface IInventoryAdjustmentService
{
    Task<IReadOnlyList<InventoryAdjustmentResponse>> GetAllAsync(CancellationToken ct = default);
    Task<InventoryAdjustmentResponse?> GetByIdAsync(int idInventoryAdjustment, CancellationToken ct = default);
    Task<InventoryAdjustmentResponse> CreateAsync(CreateInventoryAdjustmentRequest request, CancellationToken ct = default);
    Task<InventoryAdjustmentResponse?> ConfirmAsync(int idInventoryAdjustment, CancellationToken ct = default);
    Task<InventoryAdjustmentResponse?> CancelAsync(int idInventoryAdjustment, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idInventoryAdjustment, CancellationToken ct = default);
}
