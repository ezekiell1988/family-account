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
        p.IsVariantParent,
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
            IsVariantParent = request.IsVariantParent,
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
        product.IsVariantParent = request.IsVariantParent;
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

    public async Task<IReadOnlyList<VariantSummary>> GetVariantsAsync(int idProductParent, CancellationToken ct = default)
    {
        var variants = await db.Product
            .AsNoTracking()
            .Where(p => p.IdProductParent == idProductParent)
            .Include(p => p.ProductVariantAttributes)
                .ThenInclude(va => va.IdAttributeValueNavigation)
                    .ThenInclude(av => av.IdProductAttributeNavigation)
            .OrderBy(p => p.NameProduct)
            .ToListAsync(ct);

        return variants.Select(p => new VariantSummary(
            p.IdProduct,
            p.NameProduct,
            p.CodeProduct,
            p.ProductVariantAttributes
                .OrderBy(va => va.IdAttributeValueNavigation.IdProductAttributeNavigation.SortOrder)
                .Select(va => new VariantAttributeSummary(
                    va.IdAttributeValueNavigation.IdProductAttributeNavigation.NameAttribute,
                    va.IdAttributeValueNavigation.NameValue))
                .ToList()
        )).ToList();
    }

    public async Task<(GenerateVariantsResponse? Result, string? Error)> GenerateVariantsAsync(
        int idProductParent, GenerateVariantsRequest request, CancellationToken ct = default)
    {
        var parent = await db.Product
            .Include(p => p.ProductAttributes)
                .ThenInclude(a => a.AttributeValues)
            .FirstOrDefaultAsync(p => p.IdProduct == idProductParent, ct);

        if (parent is null)
            return (null, "Producto padre no encontrado.");

        var attrGroups = parent.ProductAttributes
            .OrderBy(a => a.SortOrder)
            .Select(a => a.AttributeValues
                .OrderBy(v => v.SortOrder)
                .ToList())
            .ToList();

        if (attrGroups.Count == 0 || attrGroups.Any(g => g.Count == 0))
            return (null, "El producto padre debe tener al menos un atributo con al menos un valor para generar variantes.");

        // Producto cartesiano
        IEnumerable<IReadOnlyList<AttributeValue>> combinations = new[] { Array.Empty<AttributeValue>() as IReadOnlyList<AttributeValue> };
        foreach (var group in attrGroups)
        {
            combinations = combinations
                .SelectMany(combo => group.Select(val => (IReadOnlyList<AttributeValue>)combo.Append(val).ToList()));
        }
        var allCombinations = combinations.ToList();

        // Variantes ya existentes: conjunto de IdAttributeValue por producto hijo
        var existingVariantSets = await db.ProductVariantAttribute
            .Where(va => va.IdProductNavigation.IdProductParent == idProductParent)
            .GroupBy(va => va.IdProduct)
            .Select(g => g.Select(va => va.IdAttributeValue).ToHashSet())
            .ToListAsync(ct);

        static string Normalize(string s) =>
            s.ToUpperInvariant().Replace(' ', '-');

        int created = 0;
        int skipped = 0;
        var createdVariants = new List<Product>();

        foreach (var combo in allCombinations)
        {
            var comboIds = combo.Select(v => v.IdAttributeValue).ToHashSet();
            if (existingVariantSets.Any(existing => existing.SetEquals(comboIds)))
            {
                skipped++;
                continue;
            }

            var suffixes = combo.Select(v => Normalize(v.NameValue));
            var nameSuffix  = string.Join(" ", combo.Select(v => v.NameValue));
            var codeSuffix  = string.Join("-", suffixes);

            var variant = new Product
            {
                CodeProduct     = $"{Normalize(request.CodePrefix)}-{codeSuffix}",
                NameProduct     = $"{parent.NameProduct} {nameSuffix}",
                IdProductType   = request.IdProductType,
                IdUnit          = request.IdUnit,
                IdProductParent = idProductParent,
                AverageCost     = 0m,
                IsVariantParent = false,
                HasOptions      = false,
                IsCombo         = false
            };

            db.Product.Add(variant);
            await db.SaveChangesAsync(ct);

            foreach (var val in combo)
            {
                db.ProductVariantAttribute.Add(new ProductVariantAttribute
                {
                    IdProduct        = variant.IdProduct,
                    IdAttributeValue = val.IdAttributeValue
                });
            }

            createdVariants.Add(variant);
            created++;
        }

        if (createdVariants.Count > 0)
        {
            parent.IsVariantParent = true;
        }

        await db.SaveChangesAsync(ct);

        // Cargar atributos de variantes creadas para la respuesta
        var variantIds = createdVariants.Select(v => v.IdProduct).ToList();
        var variantAttrs = await db.ProductVariantAttribute
            .AsNoTracking()
            .Where(va => variantIds.Contains(va.IdProduct))
            .Include(va => va.IdAttributeValueNavigation)
                .ThenInclude(av => av.IdProductAttributeNavigation)
            .ToListAsync(ct);

        var attrsByVariant = variantAttrs
            .GroupBy(va => va.IdProduct)
            .ToDictionary(g => g.Key, g => g.ToList());

        var variantSummaries = createdVariants.Select(v =>
        {
            var attrs = attrsByVariant.TryGetValue(v.IdProduct, out var list) ? list : [];
            return new VariantSummary(
                v.IdProduct,
                v.NameProduct,
                v.CodeProduct,
                attrs
                    .OrderBy(va => va.IdAttributeValueNavigation.IdProductAttributeNavigation.SortOrder)
                    .Select(va => new VariantAttributeSummary(
                        va.IdAttributeValueNavigation.IdProductAttributeNavigation.NameAttribute,
                        va.IdAttributeValueNavigation.NameValue))
                    .ToList());
        }).ToList();

        return (new GenerateVariantsResponse(created, skipped, variantSummaries), null);
    }
}
