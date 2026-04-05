using FamilyAccountApi.Features.InventoryLots.Dtos;

namespace FamilyAccountApi.Features.InventoryLots;

public interface IInventoryLotService
{
    Task<IReadOnlyList<InventoryLotResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default);
    Task<InventoryLotResponse?> GetByIdAsync(int idInventoryLot, CancellationToken ct = default);
    Task<decimal> GetStockTotalAsync(int idProduct, CancellationToken ct = default);
    /// <summary>
    /// Sugiere el lote más antiguo con stock disponible y no vencido — FEFO.
    /// Los lotes sin fecha de vencimiento se consideran siempre válidos y se colocan al final.
    /// </summary>
    Task<InventoryLotResponse?> GetSuggestedLotAsync(int idProduct, DateOnly referenceDate, CancellationToken ct = default);
}
