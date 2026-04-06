namespace FamilyAccountApi.Features.SalesOrders.Dtos;

/// <summary>
/// Cuerpo opcional del endpoint POST /sales-orders/{id}/confirm.
/// Si se envía IdWarehouse, el sistema procesará automáticamente
/// el ciclo completo: OP → producción → pedido completo → factura confirmada.
/// </summary>
public sealed record ConfirmSalesOrderRequest
{
    /// <summary>
    /// Bodega donde se descontarán los insumos y se creará el lote del PT.
    /// Requerido para el flujo automático de ensamble.
    /// </summary>
    public int? IdWarehouse { get; init; }
}

public sealed record SendToProductionResponse(
    IReadOnlyList<CreatedProductionOrderInfo> ProductionOrders);

public sealed record CreatedProductionOrderInfo(
    int    IdProductionOrder,
    string NumberProductionOrder,
    int    IdSalesOrderLine,
    string ProductName);

public sealed record GenerateInvoiceResponse(int IdSalesInvoice);
