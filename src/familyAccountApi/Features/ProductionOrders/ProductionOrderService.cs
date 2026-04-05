using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductionOrders.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductionOrders;

public sealed class ProductionOrderService(AppDbContext db) : IProductionOrderService
{
    private static IQueryable<ProductionOrderResponse> Project(IQueryable<ProductionOrder> q) =>
        q.Select(po => new ProductionOrderResponse(
            po.IdProductionOrder,
            po.IdFiscalPeriod,
            po.IdSalesOrder,
            po.IdSalesOrderNavigation != null ? po.IdSalesOrderNavigation.NumberOrder : null,
            po.IdSalesOrderNavigation != null ? po.IdSalesOrderNavigation.IdContactNavigation.Name : null,
            po.NumberProductionOrder,
            po.DateOrder,
            po.DateRequired,
            po.StatusProductionOrder,
            po.DescriptionOrder,
            po.CreatedAt,
            po.ProductionOrderLines.Select(l => new ProductionOrderLineResponse(
                l.IdProductionOrderLine,
                l.IdProduct,
                l.IdProductNavigation.NameProduct,
                l.IdProductUnit,
                l.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
                l.IdSalesOrderLine,
                l.QuantityRequired,
                l.QuantityProduced,
                l.QuantityRequired - l.QuantityProduced,
                l.DescriptionLine)).ToList()));

    public async Task<IReadOnlyList<ProductionOrderResponse>> GetAllAsync(CancellationToken ct = default) =>
        await Project(db.ProductionOrder.AsNoTracking().OrderByDescending(po => po.DateOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductionOrderResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default) =>
        await Project(db.ProductionOrder.AsNoTracking()
            .Where(po => po.IdFiscalPeriod == idFiscalPeriod)
            .OrderByDescending(po => po.DateOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductionOrderResponse>> GetBySalesOrderAsync(int idSalesOrder, CancellationToken ct = default) =>
        await Project(db.ProductionOrder.AsNoTracking()
            .Where(po => po.IdSalesOrder == idSalesOrder))
            .ToListAsync(ct);

    public async Task<ProductionOrderResponse?> GetByIdAsync(int idProductionOrder, CancellationToken ct = default) =>
        await Project(db.ProductionOrder.AsNoTracking()
            .Where(po => po.IdProductionOrder == idProductionOrder))
            .FirstOrDefaultAsync(ct);

    public async Task<ProductionOrderResponse> CreateAsync(CreateProductionOrderRequest request, CancellationToken ct = default)
    {
        var entity = new ProductionOrder
        {
            IdFiscalPeriod         = request.IdFiscalPeriod,
            IdSalesOrder           = request.IdSalesOrder,
            NumberProductionOrder  = "BORRADOR",
            DateOrder              = request.DateOrder,
            DateRequired           = request.DateRequired,
            StatusProductionOrder  = "Borrador",
            DescriptionOrder       = request.DescriptionOrder,
            CreatedAt              = DateTime.UtcNow
        };

        foreach (var l in request.Lines)
        {
            entity.ProductionOrderLines.Add(new ProductionOrderLine
            {
                IdProduct        = l.IdProduct,
                IdProductUnit    = l.IdProductUnit,
                IdSalesOrderLine = l.IdSalesOrderLine,
                QuantityRequired = l.QuantityRequired,
                QuantityProduced = 0m,
                DescriptionLine  = l.DescriptionLine
            });
        }

        db.ProductionOrder.Add(entity);
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(entity.IdProductionOrder, ct))!;
    }

    public async Task<ProductionOrderResponse?> UpdateAsync(int idProductionOrder, UpdateProductionOrderRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductionOrder
            .Include(po => po.ProductionOrderLines)
            .FirstOrDefaultAsync(po => po.IdProductionOrder == idProductionOrder, ct);

        if (entity is null) return null;
        if (entity.StatusProductionOrder != "Borrador")
            throw new InvalidOperationException("Solo se puede editar una orden en estado Borrador.");

        db.ProductionOrderLine.RemoveRange(entity.ProductionOrderLines);

        entity.IdFiscalPeriod   = request.IdFiscalPeriod;
        entity.IdSalesOrder     = request.IdSalesOrder;
        entity.DateOrder        = request.DateOrder;
        entity.DateRequired     = request.DateRequired;
        entity.DescriptionOrder = request.DescriptionOrder;

        foreach (var l in request.Lines)
        {
            entity.ProductionOrderLines.Add(new ProductionOrderLine
            {
                IdProduct        = l.IdProduct,
                IdProductUnit    = l.IdProductUnit,
                IdSalesOrderLine = l.IdSalesOrderLine,
                QuantityRequired = l.QuantityRequired,
                QuantityProduced = 0m,
                DescriptionLine  = l.DescriptionLine
            });
        }

        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(idProductionOrder, ct))!;
    }

    public async Task<(bool Ok, string? Error)> UpdateStatusAsync(int idProductionOrder, UpdateProductionOrderStatusRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductionOrder.FindAsync([idProductionOrder], ct);
        if (entity is null) return (false, "Orden de producción no encontrada.");

        // Transiciones válidas
        var current = entity.StatusProductionOrder;
        var newStatus = request.StatusProductionOrder;

        var allowed = (current, newStatus) switch
        {
            ("Borrador",   "Pendiente")   => true,
            ("Pendiente",  "EnProceso")   => true,
            ("EnProceso",  "Completado")  => true,
            ("Pendiente",  "Anulado")     => true,
            ("EnProceso",  "Anulado")     => true,
            ("Borrador",   "Anulado")     => true,
            _ => false
        };

        if (!allowed) return (false, $"Transición inválida: {current} → {newStatus}.");

        // Al confirmar la orden (Borrador → Pendiente), asigna número correlativo
        if (current == "Borrador" && newStatus == "Pendiente")
        {
            var count = await db.ProductionOrder.CountAsync(po => po.StatusProductionOrder != "Borrador", ct);
            entity.NumberProductionOrder = $"OP-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
        }

        entity.StatusProductionOrder = newStatus;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int idProductionOrder, CancellationToken ct = default)
    {
        var entity = await db.ProductionOrder.FindAsync([idProductionOrder], ct);
        if (entity is null) return false;
        if (entity.StatusProductionOrder != "Borrador")
            throw new InvalidOperationException("Solo se puede eliminar una orden en estado Borrador.");

        var deleted = await db.ProductionOrder
            .Where(po => po.IdProductionOrder == idProductionOrder)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }
}
