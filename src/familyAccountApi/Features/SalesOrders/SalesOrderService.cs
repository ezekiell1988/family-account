using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.SalesOrders.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.SalesOrders;

public sealed class SalesOrderService(AppDbContext db) : ISalesOrderService
{
    // ── Proyección reutilizable ──────────────────────────────────────────────
    private static SalesOrderLineResponse MapLine(SalesOrderLine l) => new(
        l.IdSalesOrderLine,
        l.IdProduct,
        l.IdProductNavigation.NameProduct,
        l.IdProductUnit,
        l.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
        l.IdPriceListItem,
        l.Quantity,
        l.QuantityBase,
        l.UnitPrice,
        l.TaxPercent,
        l.TotalLineAmount,
        l.DescriptionLine);

    private static IQueryable<SalesOrderResponse> ProjectOrder(IQueryable<SalesOrder> q) =>
        q.Select(so => new SalesOrderResponse(
            so.IdSalesOrder,
            so.IdFiscalPeriod,
            so.IdCurrency,
            so.IdCurrencyNavigation.CodeCurrency,
            so.IdContact,
            so.IdContactNavigation.Name,
            so.IdPriceList,
            so.IdPriceListNavigation != null ? so.IdPriceListNavigation.NamePriceList : null,
            so.NumberOrder,
            so.DateOrder,
            so.DateDelivery,
            so.SubTotalAmount,
            so.TaxAmount,
            so.TotalAmount,
            so.ExchangeRateValue,
            so.StatusOrder,
            so.DescriptionOrder,
            so.CreatedAt,
            so.SalesOrderLines.Select(l => new SalesOrderLineResponse(
                l.IdSalesOrderLine,
                l.IdProduct,
                l.IdProductNavigation.NameProduct,
                l.IdProductUnit,
                l.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
                l.IdPriceListItem,
                l.Quantity,
                l.QuantityBase,
                l.UnitPrice,
                l.TaxPercent,
                l.TotalLineAmount,
                l.DescriptionLine)).ToList()));

    // ── Lecturas ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesOrderResponse>> GetAllAsync(CancellationToken ct = default) =>
        await ProjectOrder(db.SalesOrder.AsNoTracking().OrderByDescending(so => so.DateOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SalesOrderResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default) =>
        await ProjectOrder(db.SalesOrder.AsNoTracking()
            .Where(so => so.IdFiscalPeriod == idFiscalPeriod)
            .OrderByDescending(so => so.DateOrder))
            .ToListAsync(ct);

    public async Task<SalesOrderResponse?> GetByIdAsync(int idSalesOrder, CancellationToken ct = default) =>
        await ProjectOrder(db.SalesOrder.AsNoTracking()
            .Where(so => so.IdSalesOrder == idSalesOrder))
            .FirstOrDefaultAsync(ct);

    // ── Creates / Updates ────────────────────────────────────────────────────

    public async Task<SalesOrderResponse> CreateAsync(CreateSalesOrderRequest request, CancellationToken ct = default)
    {
        var entity = BuildOrder(request);
        db.SalesOrder.Add(entity);
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(entity.IdSalesOrder, ct))!;
    }

    public async Task<SalesOrderResponse?> UpdateAsync(int idSalesOrder, UpdateSalesOrderRequest request, CancellationToken ct = default)
    {
        var entity = await db.SalesOrder
            .Include(so => so.SalesOrderLines)
            .FirstOrDefaultAsync(so => so.IdSalesOrder == idSalesOrder, ct);

        if (entity is null) return null;
        if (entity.StatusOrder != "Borrador")
            throw new InvalidOperationException("Solo se puede editar un pedido en estado Borrador.");

        db.SalesOrderLine.RemoveRange(entity.SalesOrderLines);

        entity.IdFiscalPeriod    = request.IdFiscalPeriod;
        entity.IdCurrency        = request.IdCurrency;
        entity.IdContact         = request.IdContact;
        entity.IdPriceList       = request.IdPriceList;
        entity.DateOrder         = request.DateOrder;
        entity.DateDelivery      = request.DateDelivery;
        entity.ExchangeRateValue = request.ExchangeRateValue;
        entity.DescriptionOrder  = request.DescriptionOrder;

        ApplyLines(entity, request.Lines);
        RecalcTotals(entity);

        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(idSalesOrder, ct))!;
    }

    // ── Estado ───────────────────────────────────────────────────────────────

    public async Task<(bool Ok, string? Error)> ConfirmAsync(int idSalesOrder, CancellationToken ct = default)
    {
        var entity = await db.SalesOrder.FindAsync([idSalesOrder], ct);
        if (entity is null) return (false, "Pedido no encontrado.");
        if (entity.StatusOrder != "Borrador") return (false, "Solo se puede confirmar un pedido en estado Borrador.");

        entity.StatusOrder = "Confirmado";
        entity.NumberOrder = await GenerateNumberAsync(ct);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(int idSalesOrder, CancellationToken ct = default)
    {
        var entity = await db.SalesOrder.FindAsync([idSalesOrder], ct);
        if (entity is null) return (false, "Pedido no encontrado.");
        if (entity.StatusOrder == "Anulado") return (false, "El pedido ya está anulado.");
        if (entity.StatusOrder == "Completado") return (false, "No se puede anular un pedido Completado.");

        entity.StatusOrder = "Anulado";
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int idSalesOrder, CancellationToken ct = default)
    {
        var entity = await db.SalesOrder.FindAsync([idSalesOrder], ct);
        if (entity is null) return false;
        if (entity.StatusOrder != "Borrador")
            throw new InvalidOperationException("Solo se puede eliminar un pedido en estado Borrador.");

        var deleted = await db.SalesOrder.Where(so => so.IdSalesOrder == idSalesOrder).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    // ── Fulfillments ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesOrderFulfillmentResponse>> GetFulfillmentsAsync(int idSalesOrder, CancellationToken ct = default) =>
        await db.SalesOrderLineFulfillment
            .AsNoTracking()
            .Where(f => f.IdSalesOrderLineNavigation.IdSalesOrder == idSalesOrder)
            .Select(f => new SalesOrderFulfillmentResponse(
                f.IdSalesOrderLineFulfillment,
                f.IdSalesOrderLine,
                f.FulfillmentType,
                f.IdInventoryLot,
                f.IdProductionOrder,
                f.IdProductionOrderNavigation != null ? f.IdProductionOrderNavigation.NumberProductionOrder : null,
                f.QuantityBase,
                f.UnitCost,
                f.CreatedAt))
            .ToListAsync(ct);

    public async Task<(SalesOrderFulfillmentResponse? Result, string? Error)> AddFulfillmentAsync(int idSalesOrder, AddFulfillmentRequest request, CancellationToken ct = default)
    {
        var line = await db.SalesOrderLine
            .FirstOrDefaultAsync(l => l.IdSalesOrderLine == request.IdSalesOrderLine && l.IdSalesOrder == idSalesOrder, ct);
        if (line is null) return (null, "Línea no encontrada en el pedido.");

        if (request.FulfillmentType == "Stock" && request.IdInventoryLot is null)
            return (null, "Para FulfillmentType 'Stock' debe indicarse IdInventoryLot.");
        if (request.FulfillmentType == "Produccion" && request.IdProductionOrder is null)
            return (null, "Para FulfillmentType 'Produccion' debe indicarse IdProductionOrder.");

        var entity = new SalesOrderLineFulfillment
        {
            IdSalesOrderLine = request.IdSalesOrderLine,
            FulfillmentType  = request.FulfillmentType,
            IdInventoryLot   = request.IdInventoryLot,
            IdProductionOrder = request.IdProductionOrder,
            QuantityBase     = request.QuantityBase,
            CreatedAt        = DateTime.UtcNow
        };

        db.SalesOrderLineFulfillment.Add(entity);
        await db.SaveChangesAsync(ct);

        var result = await db.SalesOrderLineFulfillment
            .AsNoTracking()
            .Where(f => f.IdSalesOrderLineFulfillment == entity.IdSalesOrderLineFulfillment)
            .Select(f => new SalesOrderFulfillmentResponse(
                f.IdSalesOrderLineFulfillment,
                f.IdSalesOrderLine,
                f.FulfillmentType,
                f.IdInventoryLot,
                f.IdProductionOrder,
                f.IdProductionOrderNavigation != null ? f.IdProductionOrderNavigation.NumberProductionOrder : null,
                f.QuantityBase,
                f.UnitCost,
                f.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return (result, null);
    }

    public async Task<bool> RemoveFulfillmentAsync(int idSalesOrderLineFulfillment, CancellationToken ct = default)
    {
        var deleted = await db.SalesOrderLineFulfillment
            .Where(f => f.IdSalesOrderLineFulfillment == idSalesOrderLineFulfillment)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    // ── Advances ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesOrderAdvanceResponse>> GetAdvancesAsync(int idSalesOrder, CancellationToken ct = default) =>
        await db.SalesOrderAdvance
            .AsNoTracking()
            .Where(a => a.IdSalesOrder == idSalesOrder)
            .Select(a => new SalesOrderAdvanceResponse(
                a.IdSalesOrderAdvance,
                a.IdSalesOrder,
                a.IdAccountingEntry,
                a.IdProductionOrder,
                a.IdProductionOrderNavigation != null ? a.IdProductionOrderNavigation.NumberProductionOrder : null,
                a.Amount,
                a.DateAdvance,
                a.DescriptionAdvance,
                a.CreatedAt))
            .ToListAsync(ct);

    public async Task<(SalesOrderAdvanceResponse? Result, string? Error)> AddAdvanceAsync(int idSalesOrder, CreateSalesOrderAdvanceRequest request, CancellationToken ct = default)
    {
        var order = await db.SalesOrder.FindAsync([idSalesOrder], ct);
        if (order is null) return (null, "Pedido no encontrado.");

        var entity = new SalesOrderAdvance
        {
            IdSalesOrder       = idSalesOrder,
            IdAccountingEntry  = request.IdAccountingEntry,
            IdProductionOrder  = request.IdProductionOrder,
            Amount             = request.Amount,
            DateAdvance        = request.DateAdvance,
            DescriptionAdvance = request.DescriptionAdvance,
            CreatedAt          = DateTime.UtcNow
        };

        db.SalesOrderAdvance.Add(entity);
        await db.SaveChangesAsync(ct);

        var result = await db.SalesOrderAdvance
            .AsNoTracking()
            .Where(a => a.IdSalesOrderAdvance == entity.IdSalesOrderAdvance)
            .Select(a => new SalesOrderAdvanceResponse(
                a.IdSalesOrderAdvance,
                a.IdSalesOrder,
                a.IdAccountingEntry,
                a.IdProductionOrder,
                a.IdProductionOrderNavigation != null ? a.IdProductionOrderNavigation.NumberProductionOrder : null,
                a.Amount,
                a.DateAdvance,
                a.DescriptionAdvance,
                a.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return (result, null);
    }

    public async Task<bool> RemoveAdvanceAsync(int idSalesOrderAdvance, CancellationToken ct = default)
    {
        var deleted = await db.SalesOrderAdvance
            .Where(a => a.IdSalesOrderAdvance == idSalesOrderAdvance)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    // ── Helpers privados ─────────────────────────────────────────────────────

    private static SalesOrder BuildOrder(CreateSalesOrderRequest request)
    {
        var order = new SalesOrder
        {
            IdFiscalPeriod    = request.IdFiscalPeriod,
            IdCurrency        = request.IdCurrency,
            IdContact         = request.IdContact,
            IdPriceList       = request.IdPriceList,
            NumberOrder       = "BORRADOR",
            DateOrder         = request.DateOrder,
            DateDelivery      = request.DateDelivery,
            ExchangeRateValue = request.ExchangeRateValue,
            StatusOrder       = "Borrador",
            DescriptionOrder  = request.DescriptionOrder,
            CreatedAt         = DateTime.UtcNow
        };

        ApplyLines(order, request.Lines);
        RecalcTotals(order);
        return order;
    }

    private static void ApplyLines(SalesOrder order, IReadOnlyList<SalesOrderLineRequest> lines)
    {
        foreach (var l in lines)
        {
            order.SalesOrderLines.Add(new SalesOrderLine
            {
                IdProduct       = l.IdProduct,
                IdProductUnit   = l.IdProductUnit,
                IdPriceListItem = l.IdPriceListItem,
                Quantity        = l.Quantity,
                QuantityBase    = l.Quantity,   // El ConversionFactor real se puede aplicar al confirmar
                UnitPrice       = l.UnitPrice,
                TaxPercent      = l.TaxPercent,
                TotalLineAmount = Math.Round(l.Quantity * l.UnitPrice * (1 + l.TaxPercent / 100m), 2),
                DescriptionLine = l.DescriptionLine
            });
        }
    }

    private static void RecalcTotals(SalesOrder order)
    {
        var subTotal = order.SalesOrderLines.Sum(l => Math.Round(l.Quantity * l.UnitPrice, 2));
        var taxTotal = order.SalesOrderLines.Sum(l => Math.Round(l.Quantity * l.UnitPrice * l.TaxPercent / 100m, 2));
        order.SubTotalAmount = subTotal;
        order.TaxAmount      = taxTotal;
        order.TotalAmount    = subTotal + taxTotal;
    }

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var count = await db.SalesOrder.CountAsync(so => so.StatusOrder != "Borrador", ct);
        return $"PED-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
    }
}
