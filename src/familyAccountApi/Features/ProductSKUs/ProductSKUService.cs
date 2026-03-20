using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductSKUs.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductSKUs;

public sealed class ProductSKUService(AppDbContext db) : IProductSKUService
{
    public async Task<IReadOnlyList<ProductSKUResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.ProductSKU
            .AsNoTracking()
            .Select(p => new ProductSKUResponse(
                p.IdProductSKU,
                p.CodeProductSKU,
                p.NameProductSKU,
                p.BrandProductSKU,
                p.DescriptionProductSKU,
                p.NetContent))
            .ToListAsync(ct);
    }

    public async Task<ProductSKUResponse?> GetByIdAsync(int idProductSKU, CancellationToken ct = default)
    {
        return await db.ProductSKU
            .AsNoTracking()
            .Where(p => p.IdProductSKU == idProductSKU)
            .Select(p => new ProductSKUResponse(
                p.IdProductSKU,
                p.CodeProductSKU,
                p.NameProductSKU,
                p.BrandProductSKU,
                p.DescriptionProductSKU,
                p.NetContent))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ProductSKUResponse> CreateAsync(CreateProductSKURequest request, CancellationToken ct = default)
    {
        var product = new ProductSKU
        {
            CodeProductSKU        = request.CodeProductSKU,
            NameProductSKU        = request.NameProductSKU,
            BrandProductSKU       = request.BrandProductSKU,
            DescriptionProductSKU = request.DescriptionProductSKU,
            NetContent            = request.NetContent
        };

        db.ProductSKU.Add(product);
        await db.SaveChangesAsync(ct);

        return new ProductSKUResponse(
            product.IdProductSKU,
            product.CodeProductSKU,
            product.NameProductSKU,
            product.BrandProductSKU,
            product.DescriptionProductSKU,
            product.NetContent);
    }

    public async Task<ProductSKUResponse?> UpdateAsync(int idProductSKU, UpdateProductSKURequest request, CancellationToken ct = default)
    {
        var product = await db.ProductSKU.FindAsync([idProductSKU], ct);
        if (product is null) return null;

        product.CodeProductSKU        = request.CodeProductSKU;
        product.NameProductSKU        = request.NameProductSKU;
        product.BrandProductSKU       = request.BrandProductSKU;
        product.DescriptionProductSKU = request.DescriptionProductSKU;
        product.NetContent            = request.NetContent;

        await db.SaveChangesAsync(ct);

        return new ProductSKUResponse(
            product.IdProductSKU,
            product.CodeProductSKU,
            product.NameProductSKU,
            product.BrandProductSKU,
            product.DescriptionProductSKU,
            product.NetContent);
    }

    public async Task<bool> DeleteAsync(int idProductSKU, CancellationToken ct = default)
    {
        var deleted = await db.ProductSKU
            .Where(p => p.IdProductSKU == idProductSKU)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
