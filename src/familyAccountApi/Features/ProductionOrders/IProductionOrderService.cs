using FamilyAccountApi.Features.ProductionOrders.Dtos;

namespace FamilyAccountApi.Features.ProductionOrders;

public interface IProductionOrderService
{
    Task<IReadOnlyList<ProductionOrderResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductionOrderResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default);
    Task<IReadOnlyList<ProductionOrderResponse>> GetBySalesOrderAsync(int idSalesOrder, CancellationToken ct = default);
    Task<ProductionOrderResponse?> GetByIdAsync(int idProductionOrder, CancellationToken ct = default);
    Task<ProductionOrderResponse> CreateAsync(CreateProductionOrderRequest request, CancellationToken ct = default);
    Task<ProductionOrderResponse?> UpdateAsync(int idProductionOrder, UpdateProductionOrderRequest request, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> UpdateStatusAsync(int idProductionOrder, UpdateProductionOrderStatusRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProductionOrder, CancellationToken ct = default);
}
