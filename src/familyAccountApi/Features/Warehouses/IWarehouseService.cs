using FamilyAccountApi.Features.Warehouses.Dtos;

namespace FamilyAccountApi.Features.Warehouses;

public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(CancellationToken ct = default);
    Task<WarehouseResponse?> GetByIdAsync(int idWarehouse, CancellationToken ct = default);
    Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken ct = default);
    Task<WarehouseResponse?> UpdateAsync(int idWarehouse, UpdateWarehouseRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idWarehouse, CancellationToken ct = default);
}
