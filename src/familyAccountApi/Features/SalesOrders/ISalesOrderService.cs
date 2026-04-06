using FamilyAccountApi.Features.SalesOrders.Dtos;

namespace FamilyAccountApi.Features.SalesOrders;

public interface ISalesOrderService
{
    Task<IReadOnlyList<SalesOrderResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SalesOrderResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default);
    Task<SalesOrderResponse?> GetByIdAsync(int idSalesOrder, CancellationToken ct = default);
    Task<(SalesOrderResponse? Result, string? Error)> CreateAsync(CreateSalesOrderRequest request, CancellationToken ct = default);
    Task<(SalesOrderResponse? Result, string? Error)> UpdateAsync(int idSalesOrder, UpdateSalesOrderRequest request, CancellationToken ct = default);
    /// <summary>
    /// Confirma el pedido. Si se proporciona <see cref="ConfirmSalesOrderRequest.IdWarehouse"/>,
    /// el sistema ejecuta automáticamente el ciclo completo de ensamble:
    /// OP → producción → completar pedido → factura confirmada.
    /// Retorna el IdSalesInvoice generado cuando se procesó en automático.
    /// </summary>
    Task<(bool Ok, string? Error, int? IdSalesInvoice)> ConfirmAsync(int idSalesOrder, ConfirmSalesOrderRequest? request = null, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> CancelAsync(int idSalesOrder, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idSalesOrder, CancellationToken ct = default);

    // Fulfillment
    Task<IReadOnlyList<SalesOrderFulfillmentResponse>> GetFulfillmentsAsync(int idSalesOrder, CancellationToken ct = default);
    Task<(SalesOrderFulfillmentResponse? Result, string? Error)> AddFulfillmentAsync(int idSalesOrder, AddFulfillmentRequest request, CancellationToken ct = default);
    Task<bool> RemoveFulfillmentAsync(int idSalesOrderLineFulfillment, CancellationToken ct = default);

    // Advances
    Task<IReadOnlyList<SalesOrderAdvanceResponse>> GetAdvancesAsync(int idSalesOrder, CancellationToken ct = default);
    Task<(SalesOrderAdvanceResponse? Result, string? Error)> AddAdvanceAsync(int idSalesOrder, CreateSalesOrderAdvanceRequest request, CancellationToken ct = default);
    Task<bool> RemoveAdvanceAsync(int idSalesOrderAdvance, CancellationToken ct = default);

    // C5: Flujo de pedido configurado
    Task<(SendToProductionResponse? Result, string? Error)> SendToProductionAsync(int idSalesOrder, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> CompleteOrderAsync(int idSalesOrder, CancellationToken ct = default);
    Task<(GenerateInvoiceResponse? Result, string? Error)> GenerateInvoiceAsync(int idSalesOrder, CancellationToken ct = default);
}
