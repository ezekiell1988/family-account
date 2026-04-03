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
                il.Product.IdUnitNavigation.CodeUnit,
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
            il.ExpirationDate, il.UnitCost, il.QuantityAvailable,
            il.Product.IdUnitNavigation.CodeUnit, il.SourceType,
            il.IdPurchaseInvoice, il.IdInventoryAdjustment, il.CreatedAt);
    }

    public async Task<decimal> GetStockTotalAsync(int idProduct, CancellationToken ct = default)
        => await db.InventoryLot
            .Where(il => il.IdProduct == idProduct)
            .SumAsync(il => il.QuantityAvailable, ct);
}
