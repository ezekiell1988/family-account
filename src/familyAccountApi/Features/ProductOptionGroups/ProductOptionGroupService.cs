using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductOptionGroups.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductOptionGroups;

public sealed class ProductOptionGroupService(AppDbContext db) : IProductOptionGroupService
{
    private static ProductOptionGroupResponse ToResponse(ProductOptionGroup g) => new(
        g.IdProductOptionGroup,
        g.IdProduct,
        g.NameGroup,
        g.IsRequired,
        g.MinSelections,
        g.MaxSelections,
        g.AllowSplit,
        g.SortOrder,
        g.ProductOptionItems
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductOptionItemResponse(
                i.IdProductOptionItem,
                i.NameItem,
                i.PriceDelta,
                i.IsDefault,
                i.SortOrder))
            .ToList());

    private static IQueryable<ProductOptionGroup> WithIncludes(IQueryable<ProductOptionGroup> q)
        => q.Include(g => g.ProductOptionItems);

    public async Task<IReadOnlyList<ProductOptionGroupResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default)
    {
        var list = await WithIncludes(db.ProductOptionGroup.AsNoTracking())
            .Where(g => g.IdProduct == idProduct)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<ProductOptionGroupResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var group = await WithIncludes(db.ProductOptionGroup.AsNoTracking())
            .FirstOrDefaultAsync(g => g.IdProductOptionGroup == id, ct);

        return group is null ? null : ToResponse(group);
    }

    public async Task<(ProductOptionGroupResponse result, string? error)> CreateAsync(
        CreateProductOptionGroupRequest request, CancellationToken ct = default)
    {
        // V1: solo crear en productos con HasOptions = true
        var product = await db.Product.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProduct == request.IdProduct, ct);

        if (product is null)
            return (null!, "Producto no encontrado.");

        if (!product.HasOptions)
            return (null!, "El producto no tiene HasOptions habilitado.");

        // V2: al menos 1 item
        if (request.Items.Count == 0)
            return (null!, "El grupo debe tener al menos un item.");

        // V3: MinSelections <= MaxSelections
        if (request.MinSelections > request.MaxSelections)
            return (null!, "MinSelections no puede ser mayor que MaxSelections.");

        // V4: si no es requerido, MinSelections debe ser 0
        if (!request.IsRequired && request.MinSelections != 0)
            return (null!, "Si el grupo no es requerido, MinSelections debe ser 0.");

        var group = new ProductOptionGroup
        {
            IdProduct      = request.IdProduct,
            NameGroup      = request.NameGroup,
            IsRequired     = request.IsRequired,
            MinSelections  = request.MinSelections,
            MaxSelections  = request.MaxSelections,
            AllowSplit     = request.AllowSplit,
            SortOrder      = request.SortOrder,
            ProductOptionItems = request.Items.Select(i => new ProductOptionItem
            {
                NameItem   = i.NameItem,
                PriceDelta = i.PriceDelta,
                IsDefault  = i.IsDefault,
                SortOrder  = i.SortOrder
            }).ToList()
        };

        db.ProductOptionGroup.Add(group);
        await db.SaveChangesAsync(ct);

        var created = await WithIncludes(db.ProductOptionGroup.AsNoTracking())
            .FirstAsync(g => g.IdProductOptionGroup == group.IdProductOptionGroup, ct);

        return (ToResponse(created), null);
    }

    public async Task<(ProductOptionGroupResponse? result, string? error)> UpdateAsync(
        int id, UpdateProductOptionGroupRequest request, CancellationToken ct = default)
    {
        var group = await db.ProductOptionGroup
            .Include(g => g.ProductOptionItems)
            .FirstOrDefaultAsync(g => g.IdProductOptionGroup == id, ct);

        if (group is null) return (null, null);

        // V2: al menos 1 item
        if (request.Items.Count == 0)
            return (null!, "El grupo debe tener al menos un item.");

        // V3
        if (request.MinSelections > request.MaxSelections)
            return (null!, "MinSelections no puede ser mayor que MaxSelections.");

        // V4
        if (!request.IsRequired && request.MinSelections != 0)
            return (null!, "Si el grupo no es requerido, MinSelections debe ser 0.");

        group.NameGroup     = request.NameGroup;
        group.IsRequired    = request.IsRequired;
        group.MinSelections = request.MinSelections;
        group.MaxSelections = request.MaxSelections;
        group.AllowSplit    = request.AllowSplit;
        group.SortOrder     = request.SortOrder;

        // Reemplazar items
        db.ProductOptionItem.RemoveRange(group.ProductOptionItems);
        group.ProductOptionItems = request.Items.Select(i => new ProductOptionItem
        {
            IdProductOptionGroup = group.IdProductOptionGroup,
            NameItem             = i.NameItem,
            PriceDelta           = i.PriceDelta,
            IsDefault            = i.IsDefault,
            SortOrder            = i.SortOrder
        }).ToList();

        await db.SaveChangesAsync(ct);

        var updated = await WithIncludes(db.ProductOptionGroup.AsNoTracking())
            .FirstAsync(g => g.IdProductOptionGroup == id, ct);

        return (ToResponse(updated), null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var group = await db.ProductOptionGroup.FindAsync([id], ct);
        if (group is null) return false;

        db.ProductOptionGroup.Remove(group);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
