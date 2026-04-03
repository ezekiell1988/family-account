using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.InventoryAdjustments.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.InventoryAdjustments;

public sealed class InventoryAdjustmentService(AppDbContext db) : IInventoryAdjustmentService
{
    private static InventoryAdjustmentResponse ToResponse(InventoryAdjustment ia) => new(
        ia.IdInventoryAdjustment,
        ia.IdFiscalPeriod,
        ia.TypeAdjustment,
        ia.NumberAdjustment,
        ia.DateAdjustment,
        ia.DescriptionAdjustment,
        ia.StatusAdjustment,
        ia.CreatedAt,
        ia.InventoryAdjustmentLines.Select(l => new InventoryAdjustmentLineResponse(
            l.IdInventoryAdjustmentLine,
            l.IdInventoryLot,
            l.IdInventoryLotNavigation.Product.NameProduct,
            l.IdInventoryLotNavigation.LotNumber,
            l.QuantityDelta,
            l.UnitCostNew,
            l.DescriptionLine)).ToList());

    private static IQueryable<InventoryAdjustment> WithIncludes(IQueryable<InventoryAdjustment> q)
        => q.Include(ia => ia.InventoryAdjustmentLines)
               .ThenInclude(l => l.IdInventoryLotNavigation)
               .ThenInclude(il => il.Product);

    public async Task<IReadOnlyList<InventoryAdjustmentResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .OrderByDescending(ia => ia.DateAdjustment)
            .ThenByDescending(ia => ia.IdInventoryAdjustment)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<InventoryAdjustmentResponse?> GetByIdAsync(int idInventoryAdjustment, CancellationToken ct = default)
    {
        var ia = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .FirstOrDefaultAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        return ia is null ? null : ToResponse(ia);
    }

    public async Task<InventoryAdjustmentResponse> CreateAsync(CreateInventoryAdjustmentRequest request, CancellationToken ct = default)
    {
        var adjustment = new InventoryAdjustment
        {
            IdFiscalPeriod        = request.IdFiscalPeriod,
            TypeAdjustment        = request.TypeAdjustment,
            NumberAdjustment      = "BORRADOR",   // se reemplaza al confirmar
            DateAdjustment        = request.DateAdjustment,
            DescriptionAdjustment = request.DescriptionAdjustment,
            StatusAdjustment      = "Borrador",
            CreatedAt             = DateTime.UtcNow,
            InventoryAdjustmentLines = request.Lines.Select(l => new InventoryAdjustmentLine
            {
                IdInventoryLot  = l.IdInventoryLot,
                QuantityDelta   = l.QuantityDelta,
                UnitCostNew     = l.UnitCostNew,
                DescriptionLine = l.DescriptionLine
            }).ToList()
        };

        db.InventoryAdjustment.Add(adjustment);
        await db.SaveChangesAsync(ct);

        var created = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .FirstAsync(ia => ia.IdInventoryAdjustment == adjustment.IdInventoryAdjustment, ct);

        return ToResponse(created);
    }

    public async Task<InventoryAdjustmentResponse?> ConfirmAsync(int idInventoryAdjustment, CancellationToken ct = default)
    {
        var adjustment = await db.InventoryAdjustment
            .Include(ia => ia.InventoryAdjustmentLines)
                .ThenInclude(l => l.IdInventoryLotNavigation)
                    .ThenInclude(il => il.Product)
            .FirstOrDefaultAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        if (adjustment is null || adjustment.StatusAdjustment != "Borrador") return null;

        // Generar número de ajuste
        var date   = adjustment.DateAdjustment;
        var prefix = $"AJ-{date.Year:D4}{date.Month:D2}{date.Day:D2}";
        var count  = await db.InventoryAdjustment
            .CountAsync(ia => ia.NumberAdjustment.StartsWith(prefix) && ia.StatusAdjustment == "Confirmado", ct);

        adjustment.NumberAdjustment = $"{prefix}-{(count + 1):D3}";
        adjustment.StatusAdjustment = "Confirmado";

        // Aplicar deltas y actualizar costos
        foreach (var line in adjustment.InventoryAdjustmentLines)
        {
            var lot = line.IdInventoryLotNavigation;

            if (line.QuantityDelta != 0)
            {
                var newQty = lot.QuantityAvailable + line.QuantityDelta;
                if (newQty < 0)
                    throw new InvalidOperationException(
                        $"El lote {lot.IdInventoryLot} quedaría con stock negativo ({newQty}).");

                lot.QuantityAvailable = newQty;
            }

            if (line.UnitCostNew.HasValue)
                lot.UnitCost = line.UnitCostNew.Value;

            // Recalcular averageCost si el delta es positivo o se actualizó el costo
            if (line.QuantityDelta > 0 || line.UnitCostNew.HasValue)
            {
                var product = lot.Product;
                var allLots = await db.InventoryLot
                    .Where(il => il.IdProduct == product.IdProduct)
                    .ToListAsync(ct);

                var totalQty  = allLots.Sum(il => il.QuantityAvailable);
                var totalCost = allLots.Sum(il => il.QuantityAvailable * il.UnitCost);

                product.AverageCost = totalQty > 0 ? totalCost / totalQty : 0m;
            }
        }

        await db.SaveChangesAsync(ct);

        var confirmed = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .FirstAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        return ToResponse(confirmed);
    }

    public async Task<InventoryAdjustmentResponse?> CancelAsync(int idInventoryAdjustment, CancellationToken ct = default)
    {
        var adjustment = await db.InventoryAdjustment
            .FirstOrDefaultAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        if (adjustment is null || adjustment.StatusAdjustment == "Anulado") return null;

        adjustment.StatusAdjustment = "Anulado";
        await db.SaveChangesAsync(ct);

        var cancelled = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .FirstAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        return ToResponse(cancelled);
    }

    public async Task<bool> DeleteAsync(int idInventoryAdjustment, CancellationToken ct = default)
    {
        var adjustment = await db.InventoryAdjustment
            .FirstOrDefaultAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        if (adjustment is null) return false;
        if (adjustment.StatusAdjustment != "Borrador")
            throw new InvalidOperationException("Solo se pueden eliminar ajustes en estado Borrador.");

        db.InventoryAdjustment.Remove(adjustment);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
