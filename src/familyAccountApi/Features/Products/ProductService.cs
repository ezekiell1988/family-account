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
        p.IsCombo,
        p.ReorderPoint,
        p.SafetyStock,
        p.ReorderQuantity,
        p.ClassificationAbc);

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
            IsCombo         = request.IsCombo,
            ReorderPoint    = request.ReorderPoint,
            SafetyStock     = request.SafetyStock,
            ReorderQuantity = request.ReorderQuantity
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
        product.ReorderPoint    = request.ReorderPoint;
        product.SafetyStock     = request.SafetyStock;
        product.ReorderQuantity = request.ReorderQuantity;

        await db.SaveChangesAsync(ct);

        await db.Entry(product).Reference(p => p.IdProductTypeNavigation).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.IdUnitNavigation).LoadAsync(ct);

        return ToResponse(product);
    }

    public async Task<(bool Deleted, string? ConflictMessage)> DeleteAsync(int idProduct, CancellationToken ct = default)
    {
        // V7: verificar si hay dependencias antes de borrar
        var activeLots = await db.InventoryLot
            .AnyAsync(il => il.IdProduct == idProduct && il.QuantityAvailable > 0, ct);
        if (activeLots)
            return (false, "El producto tiene lotes activos con stock disponible. Ajuste el inventario antes de eliminar.");

        var hasInvoiceLines = await db.PurchaseInvoiceLine
            .AnyAsync(l => l.IdProduct == idProduct, ct);
        if (hasInvoiceLines)
            return (false, "El producto está referenciado en líneas de facturas de compra. No se puede eliminar.");

        var hasRecipeLines = await db.ProductRecipeLine
            .AnyAsync(rl => rl.IdProductInput == idProduct, ct);
        if (hasRecipeLines)
            return (false, "El producto es un insumo en recetas de producción. Elimine o actualice las recetas antes de continuar.");

        var deleted = await db.Product
            .Where(p => p.IdProduct == idProduct)
            .ExecuteDeleteAsync(ct);

        return (deleted > 0, null);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetBelowReorderPointAsync(CancellationToken ct = default)
    {
        // Obtiene productos cuyo stock total sea menor al punto de reorden configurado
        var products = await db.Product
            .AsNoTracking()
            .Include(p => p.IdProductTypeNavigation)
            .Include(p => p.IdUnitNavigation)
            .Where(p => p.ReorderPoint != null)
            .ToListAsync(ct);

        // Stock total por producto (suma de lotes disponibles)
        var productIds = products.Select(p => p.IdProduct).ToList();
        var stockByProduct = await db.InventoryLot
            .Where(il => productIds.Contains(il.IdProduct))
            .GroupBy(il => il.IdProduct)
            .Select(g => new { IdProduct = g.Key, Total = g.Sum(il => il.QuantityAvailable) })
            .ToDictionaryAsync(x => x.IdProduct, x => x.Total, ct);

        return products
            .Where(p =>
            {
                var stock = stockByProduct.TryGetValue(p.IdProduct, out var s) ? s : 0m;
                return stock < p.ReorderPoint!.Value;
            })
            .Select(ToResponse)
            .ToList();
    }
}
