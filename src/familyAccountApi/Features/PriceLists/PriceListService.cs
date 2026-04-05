using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.PriceLists.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.PriceLists;

public sealed class PriceListService(AppDbContext db) : IPriceListService
{
    public async Task<IReadOnlyList<PriceListResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.PriceList
            .AsNoTracking()
            .OrderByDescending(pl => pl.DateFrom)
            .Select(pl => new PriceListResponse(
                pl.IdPriceList,
                pl.NamePriceList,
                pl.Description,
                pl.DateFrom,
                pl.DateTo,
                pl.IsActive,
                pl.CreatedAt,
                pl.PriceListItems.Select(i => new PriceListItemResponse(
                    i.IdPriceListItem,
                    i.IdProduct,
                    i.IdProductNavigation.NameProduct,
                    i.IdProductUnit,
                    i.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
                    i.UnitPrice,
                    i.IsActive)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<PriceListResponse?> GetByIdAsync(int idPriceList, CancellationToken ct = default)
    {
        return await db.PriceList
            .AsNoTracking()
            .Where(pl => pl.IdPriceList == idPriceList)
            .Select(pl => new PriceListResponse(
                pl.IdPriceList,
                pl.NamePriceList,
                pl.Description,
                pl.DateFrom,
                pl.DateTo,
                pl.IsActive,
                pl.CreatedAt,
                pl.PriceListItems.Select(i => new PriceListItemResponse(
                    i.IdPriceListItem,
                    i.IdProduct,
                    i.IdProductNavigation.NameProduct,
                    i.IdProductUnit,
                    i.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
                    i.UnitPrice,
                    i.IsActive)).ToList()))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<PriceListItemResponse>> GetItemsByProductAsync(int idProduct, CancellationToken ct = default)
    {
        return await db.PriceListItem
            .AsNoTracking()
            .Where(i => i.IdProduct == idProduct && i.IsActive && i.IdPriceListNavigation.IsActive)
            .Select(i => new PriceListItemResponse(
                i.IdPriceListItem,
                i.IdProduct,
                i.IdProductNavigation.NameProduct,
                i.IdProductUnit,
                i.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
                i.UnitPrice,
                i.IsActive))
            .ToListAsync(ct);
    }

    public async Task<PriceListResponse> CreateAsync(CreatePriceListRequest request, CancellationToken ct = default)
    {
        var entity = new PriceList
        {
            NamePriceList = request.NamePriceList,
            Description   = request.Description,
            DateFrom      = request.DateFrom,
            DateTo        = request.DateTo,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            entity.PriceListItems.Add(new PriceListItem
            {
                IdProduct     = item.IdProduct,
                IdProductUnit = item.IdProductUnit,
                UnitPrice     = item.UnitPrice,
                IsActive      = true,
                CreatedAt     = DateTime.UtcNow
            });
        }

        db.PriceList.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.IdPriceList, ct))!;
    }

    public async Task<PriceListResponse?> UpdateAsync(int idPriceList, UpdatePriceListRequest request, CancellationToken ct = default)
    {
        var entity = await db.PriceList
            .Include(pl => pl.PriceListItems)
            .FirstOrDefaultAsync(pl => pl.IdPriceList == idPriceList, ct);

        if (entity is null) return null;

        entity.NamePriceList = request.NamePriceList;
        entity.Description   = request.Description;
        entity.DateFrom      = request.DateFrom;
        entity.DateTo        = request.DateTo;

        // Reemplazar ítems
        db.PriceListItem.RemoveRange(entity.PriceListItems);
        foreach (var item in request.Items)
        {
            entity.PriceListItems.Add(new PriceListItem
            {
                IdProduct     = item.IdProduct,
                IdProductUnit = item.IdProductUnit,
                UnitPrice     = item.UnitPrice,
                IsActive      = true,
                CreatedAt     = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(idPriceList, ct))!;
    }

    public async Task<bool> DeleteAsync(int idPriceList, CancellationToken ct = default)
    {
        var deleted = await db.PriceList
            .Where(pl => pl.IdPriceList == idPriceList)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
