using FamilyAccountApi.Features.InventoryAdjustments.Dtos;

namespace FamilyAccountApi.Features.InventoryAdjustments;

public interface IInventoryAdjustmentService
{
    Task<IReadOnlyList<InventoryAdjustmentResponse>> GetAllAsync(CancellationToken ct = default);
    Task<InventoryAdjustmentResponse?> GetByIdAsync(int idInventoryAdjustment, CancellationToken ct = default);
    Task<InventoryAdjustmentResponse> CreateAsync(CreateInventoryAdjustmentRequest request, CancellationToken ct = default);
    Task<InventoryAdjustmentResponse?> ConfirmAsync(int idInventoryAdjustment, CancellationToken ct = default);
    Task<(InventoryAdjustmentResponse? Result, string? ConflictMessage)> CancelAsync(int idInventoryAdjustment, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idInventoryAdjustment, CancellationToken ct = default);

    /// <summary>
    /// Genera una vista previa del conteo cíclico sin persistir nada.
    /// Calcula quantityDelta = quantityPhysical – quantityAvailable por lote.
    /// </summary>
    Task<CycleCountPreviewResponse> PreviewCycleCountAsync(CreateCycleCountRequest request, CancellationToken ct = default);

    /// <summary>
    /// Crea (y opcionalmente confirma) un ajuste de inventario tipo CONTEO
    /// a partir de cantidades físicas contadas. Calcula el delta automáticamente.
    /// Si autoConfirm=true el ajuste queda confirmado en la misma operación.
    /// </summary>
    Task<InventoryAdjustmentResponse> CreateCycleCountAsync(CreateCycleCountRequest request, bool autoConfirm, CancellationToken ct = default);
}
