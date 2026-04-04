using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Products.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Products;

public sealed class ProductService(AppDbContext db) : IProductService
{
    private static ProductResponse ToResponse(Product p) => new(
        p.IdProduct,
        p.CodeProduct,
        p.NameProduct,
        p.IdProductType,
        p.IdProductTypeNavigation.NameProductType,
        p.IdUnit,
        p.IdUnitNavigation.CodeUnit,
        p.IdProductParent,
        p.AverageCost,
        p.HasOptions,
        p.IsCombo);

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await db.Product
            .AsNoTracking()
            .Include(p => p.IdProductTypeNavigation)
            .Include(p => p.IdUnitNavigation)
            .OrderBy(p => p.NameProduct)
            .ToListAsync(ct);

        return products.Select(ToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(int idProduct, CancellationToken ct = default)
    {
        var product = await db.Product
            .AsNoTracking()
            .Include(p => p.IdProductTypeNavigation)
            .Include(p => p.IdUnitNavigation)
            .FirstOrDefaultAsync(p => p.IdProduct == idProduct, ct);

        return product is null ? null : ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            CodeProduct     = request.CodeProduct,
            NameProduct     = request.NameProduct,
            IdProductType   = request.IdProductType,
            IdUnit          = request.IdUnit,
            IdProductParent = request.IdProductParent,
            AverageCost     = 0m,
            HasOptions      = request.HasOptions,
            IsCombo         = request.IsCombo
        };

        db.Product.Add(product);
        await db.SaveChangesAsync(ct);

        await db.Entry(product).Reference(p => p.IdProductTypeNavigation).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.IdUnitNavigation).LoadAsync(ct);

        return ToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(int idProduct, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await db.Product
            .Include(p => p.IdProductTypeNavigation)
            .Include(p => p.IdUnitNavigation)
            .FirstOrDefaultAsync(p => p.IdProduct == idProduct, ct);

        if (product is null) return null;

        product.CodeProduct     = request.CodeProduct;
        product.NameProduct     = request.NameProduct;
        product.IdProductType   = request.IdProductType;
        product.IdUnit          = request.IdUnit;
        product.IdProductParent = request.IdProductParent;
        product.HasOptions      = request.HasOptions;
        product.IsCombo         = request.IsCombo;

        await db.SaveChangesAsync(ct);

        await db.Entry(product).Reference(p => p.IdProductTypeNavigation).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.IdUnitNavigation).LoadAsync(ct);

        return ToResponse(product);
    }

    public async Task<bool> DeleteAsync(int idProduct, CancellationToken ct = default)
    {
        var deleted = await db.Product
            .Where(p => p.IdProduct == idProduct)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
