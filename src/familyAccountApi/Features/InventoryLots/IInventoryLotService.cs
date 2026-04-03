using FamilyAccountApi.Features.InventoryLots.Dtos;

namespace FamilyAccountApi.Features.InventoryLots;

public interface IInventoryLotService
{
    Task<IReadOnlyList<InventoryLotResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default);
    Task<InventoryLotResponse?> GetByIdAsync(int idInventoryLot, CancellationToken ct = default);
    Task<decimal> GetStockTotalAsync(int idProduct, CancellationToken ct = default);
}
