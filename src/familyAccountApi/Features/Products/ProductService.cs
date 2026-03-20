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
        p.ProductProductSKUs
            .Select(pp => new ProductSKUSummary(
                pp.ProductSKU.IdProductSKU,
                pp.ProductSKU.CodeProductSKU,
                pp.ProductSKU.NameProductSKU,
                pp.ProductSKU.BrandProductSKU,
                pp.ProductSKU.NetContent))
            .ToList());

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await db.Product
            .AsNoTracking()
            .Include(p => p.ProductProductSKUs)
                .ThenInclude(pp => pp.ProductSKU)
            .ToListAsync(ct);

        return products.Select(ToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(int idProduct, CancellationToken ct = default)
    {
        var product = await db.Product
            .AsNoTracking()
            .Include(p => p.ProductProductSKUs)
                .ThenInclude(pp => pp.ProductSKU)
            .FirstOrDefaultAsync(p => p.IdProduct == idProduct, ct);

        return product is null ? null : ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            CodeProduct = request.CodeProduct,
            NameProduct = request.NameProduct
        };

        db.Product.Add(product);
        await db.SaveChangesAsync(ct);

        return new ProductResponse(product.IdProduct, product.CodeProduct, product.NameProduct, []);
    }

    public async Task<ProductResponse?> UpdateAsync(int idProduct, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await db.Product
            .Include(p => p.ProductProductSKUs)
                .ThenInclude(pp => pp.ProductSKU)
            .FirstOrDefaultAsync(p => p.IdProduct == idProduct, ct);

        if (product is null) return null;

        product.CodeProduct = request.CodeProduct;
        product.NameProduct = request.NameProduct;

        await db.SaveChangesAsync(ct);

        return ToResponse(product);
    }

    public async Task<bool> DeleteAsync(int idProduct, CancellationToken ct = default)
    {
        var deleted = await db.Product
            .Where(p => p.IdProduct == idProduct)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    public async Task<bool> AddSKUAsync(int idProduct, int idProductSKU, CancellationToken ct = default)
    {
        var productExists = await db.Product.AnyAsync(p => p.IdProduct == idProduct, ct);
        var skuExists     = await db.ProductSKU.AnyAsync(s => s.IdProductSKU == idProductSKU, ct);

        if (!productExists || !skuExists) return false;

        var alreadyLinked = await db.ProductProductSKU
            .AnyAsync(pp => pp.IdProduct == idProduct && pp.IdProductSKU == idProductSKU, ct);

        if (alreadyLinked) return true;

        db.ProductProductSKU.Add(new ProductProductSKU
        {
            IdProduct    = idProduct,
            IdProductSKU = idProductSKU
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveSKUAsync(int idProduct, int idProductSKU, CancellationToken ct = default)
    {
        var deleted = await db.ProductProductSKU
            .Where(pp => pp.IdProduct == idProduct && pp.IdProductSKU == idProductSKU)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
