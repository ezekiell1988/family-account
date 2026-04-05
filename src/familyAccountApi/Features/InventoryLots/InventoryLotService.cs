using FamilyAccountApi.Features.InventoryLots.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.InventoryLots;

public sealed class InventoryLotService(AppDbContext db) : IInventoryLotService
{
    public async Task<IReadOnlyList<InventoryLotResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default)
        => await db.InventoryLot
            .AsNoTracking()
            .Include(il => il.Product).ThenInclude(p => p.IdUnitNavigation)
            .Where(il => il.IdProduct == idProduct)
            .OrderBy(il =>
                il.ExpirationDate == null ? 1 : 0)
            .ThenBy(il => il.ExpirationDate)
            .ThenBy(il => il.IdInventoryLot)
            .Select(il => new InventoryLotResponse(
                il.IdInventoryLot,
                il.IdProduct,
                il.Product.NameProduct,
                il.LotNumber,
                il.ExpirationDate,
                il.UnitCost,
                il.QuantityAvailable,
                il.QuantityReserved,
                il.QuantityAvailable - il.QuantityReserved,
                il.Product.IdUnitNavigation.CodeUnit,
                il.StatusLot,
                il.SourceType,
                il.IdPurchaseInvoice,
                il.IdInventoryAdjustment,
                il.CreatedAt))
            .ToListAsync(ct);

    public async Task<InventoryLotResponse?> GetByIdAsync(int idInventoryLot, CancellationToken ct = default)
    {
        var il = await db.InventoryLot
            .AsNoTracking()
            .Include(il => il.Product).ThenInclude(p => p.IdUnitNavigation)
            .FirstOrDefaultAsync(il => il.IdInventoryLot == idInventoryLot, ct);

        if (il is null) return null;

        return new InventoryLotResponse(
            il.IdInventoryLot, il.IdProduct, il.Product.NameProduct, il.LotNumber,
            il.ExpirationDate, il.UnitCost, il.QuantityAvailable, il.QuantityReserved,
            il.QuantityAvailable - il.QuantityReserved,
            il.Product.IdUnitNavigation.CodeUnit, il.StatusLot, il.SourceType,
            il.IdPurchaseInvoice, il.IdInventoryAdjustment, il.CreatedAt);
    }

    public async Task<decimal> GetStockTotalAsync(int idProduct, CancellationToken ct = default)
        => await db.InventoryLot
            .Where(il => il.IdProduct == idProduct)
            .SumAsync(il => il.QuantityAvailable, ct);

    public async Task<InventoryLotResponse?> GetSuggestedLotAsync(
        int idProduct, DateOnly referenceDate, CancellationToken ct = default)
        => await db.InventoryLot
            .AsNoTracking()
            .Include(il => il.Product).ThenInclude(p => p.IdUnitNavigation)
            .Where(il => il.IdProduct == idProduct
                      && il.StatusLot == "Disponible"           // solo lotes en estado disponible
                      && il.QuantityAvailable > il.QuantityReserved   // stock neto > 0
                      && (il.ExpirationDate == null || il.ExpirationDate >= referenceDate))
            .OrderBy(il => il.ExpirationDate == null ? 1 : 0)  // primero lotes con vencimiento
            .ThenBy(il => il.ExpirationDate)                   // FEFO
            .ThenBy(il => il.IdInventoryLot)                   // FIFO de respaldo
            .Select(il => new InventoryLotResponse(
                il.IdInventoryLot,
                il.IdProduct,
                il.Product.NameProduct,
                il.LotNumber,
                il.ExpirationDate,
                il.UnitCost,
                il.QuantityAvailable,
                il.QuantityReserved,
                il.QuantityAvailable - il.QuantityReserved,
                il.Product.IdUnitNavigation.CodeUnit,
                il.StatusLot,
                il.SourceType,
                il.IdPurchaseInvoice,
                il.IdInventoryAdjustment,
                il.CreatedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<InventoryLotResponse?> UpdateStatusAsync(
        int idInventoryLot, UpdateInventoryLotStatusRequest request, CancellationToken ct = default)
    {
        var lot = await db.InventoryLot
            .Include(il => il.Product).ThenInclude(p => p.IdUnitNavigation)
            .FirstOrDefaultAsync(il => il.IdInventoryLot == idInventoryLot, ct);

        if (lot is null) return null;

        lot.StatusLot = request.StatusLot;
        await db.SaveChangesAsync(ct);

        return new InventoryLotResponse(
            lot.IdInventoryLot, lot.IdProduct, lot.Product.NameProduct, lot.LotNumber,
            lot.ExpirationDate, lot.UnitCost, lot.QuantityAvailable, lot.QuantityReserved,
            lot.QuantityAvailable - lot.QuantityReserved,
            lot.Product.IdUnitNavigation.CodeUnit, lot.StatusLot, lot.SourceType,
            lot.IdPurchaseInvoice, lot.IdInventoryAdjustment, lot.CreatedAt);
    }
}
