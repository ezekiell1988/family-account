using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductCategories.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductCategories;

public sealed class ProductCategoryService(AppDbContext db) : IProductCategoryService
{
    public async Task<IReadOnlyList<ProductCategoryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.ProductCategory
            .AsNoTracking()
            .Select(c => new ProductCategoryResponse(c.IdProductCategory, c.NameProductCategory))
            .ToListAsync(ct);
    }

    public async Task<ProductCategoryResponse?> GetByIdAsync(int idProductCategory, CancellationToken ct = default)
    {
        return await db.ProductCategory
            .AsNoTracking()
            .Where(c => c.IdProductCategory == idProductCategory)
            .Select(c => new ProductCategoryResponse(c.IdProductCategory, c.NameProductCategory))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ProductCategoryResponse> CreateAsync(CreateProductCategoryRequest request, CancellationToken ct = default)
    {
        var entity = new ProductCategory { NameProductCategory = request.NameProductCategory };

        db.ProductCategory.Add(entity);
        await db.SaveChangesAsync(ct);

        return new ProductCategoryResponse(entity.IdProductCategory, entity.NameProductCategory);
    }

    public async Task<ProductCategoryResponse?> UpdateAsync(int idProductCategory, UpdateProductCategoryRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductCategory.FindAsync([idProductCategory], ct);
        if (entity is null) return null;

        entity.NameProductCategory = request.NameProductCategory;
        await db.SaveChangesAsync(ct);

        return new ProductCategoryResponse(entity.IdProductCategory, entity.NameProductCategory);
    }

    public async Task<bool> DeleteAsync(int idProductCategory, CancellationToken ct = default)
    {
        var deleted = await db.ProductCategory
            .Where(c => c.IdProductCategory == idProductCategory)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    public async Task<bool> AddToProductAsync(int idProduct, int idProductCategory, CancellationToken ct = default)
    {
        var productExists  = await db.Product.AnyAsync(p => p.IdProduct == idProduct, ct);
        var categoryExists = await db.ProductCategory.AnyAsync(c => c.IdProductCategory == idProductCategory, ct);

        if (!productExists || !categoryExists) return false;

        var alreadyLinked = await db.ProductProductCategory
            .AnyAsync(pp => pp.IdProduct == idProduct && pp.IdProductCategory == idProductCategory, ct);

        if (alreadyLinked) return true;

        db.ProductProductCategory.Add(new ProductProductCategory
        {
            IdProduct         = idProduct,
            IdProductCategory = idProductCategory
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveFromProductAsync(int idProduct, int idProductCategory, CancellationToken ct = default)
    {
        var deleted = await db.ProductProductCategory
            .Where(pp => pp.IdProduct == idProduct && pp.IdProductCategory == idProductCategory)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
