using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductRecipes.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductRecipes;

public sealed class ProductRecipeService(AppDbContext db) : IProductRecipeService
{
    private static ProductRecipeResponse ToResponse(ProductRecipe r) => new(
        r.IdProductRecipe,
        r.IdProductOutput,
        r.IdProductOutputNavigation.NameProduct,
        r.NameRecipe,
        r.QuantityOutput,
        r.IdProductOutputNavigation.IdUnitNavigation.CodeUnit,
        r.DescriptionRecipe,
        r.IsActive,
        r.CreatedAt,
        r.ProductRecipeLines
            .OrderBy(l => l.SortOrder)
            .Select(l => new ProductRecipeLineResponse(
                l.IdProductRecipeLine,
                l.IdProductInput,
                l.IdProductInputNavigation.NameProduct,
                l.QuantityInput,
                l.IdProductInputNavigation.IdUnitNavigation.CodeUnit,
                l.SortOrder))
            .ToList());

    private static IQueryable<ProductRecipe> WithIncludes(IQueryable<ProductRecipe> query)
        => query
            .Include(r => r.IdProductOutputNavigation)
                .ThenInclude(p => p.IdUnitNavigation)
            .Include(r => r.ProductRecipeLines)
                .ThenInclude(l => l.IdProductInputNavigation)
                    .ThenInclude(p => p.IdUnitNavigation);

    public async Task<IReadOnlyList<ProductRecipeResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await WithIncludes(db.ProductRecipe.AsNoTracking())
            .OrderBy(r => r.NameRecipe)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<ProductRecipeResponse>> GetByProductOutputAsync(int idProductOutput, CancellationToken ct = default)
    {
        var list = await WithIncludes(db.ProductRecipe.AsNoTracking())
            .Where(r => r.IdProductOutput == idProductOutput)
            .OrderBy(r => r.NameRecipe)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<ProductRecipeResponse?> GetByIdAsync(int idProductRecipe, CancellationToken ct = default)
    {
        var recipe = await WithIncludes(db.ProductRecipe.AsNoTracking())
            .FirstOrDefaultAsync(r => r.IdProductRecipe == idProductRecipe, ct);

        return recipe is null ? null : ToResponse(recipe);
    }

    public async Task<ProductRecipeResponse> CreateAsync(CreateProductRecipeRequest request, CancellationToken ct = default)
    {
        // V4: el producto output no puede ser de tipo Materia Prima (Id=1) o Reventa (Id=4)
        var outputProduct = await db.Product.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProduct == request.IdProductOutput, ct);
        if (outputProduct is null)
            throw new InvalidOperationException($"Producto output {request.IdProductOutput} no encontrado.");
        if (outputProduct.IdProductType is 1 or 4)
            throw new InvalidOperationException(
                "El producto output no puede ser de tipo 'Materia Prima' o 'Reventa'. Use 'Producto en Proceso' o 'Producto Terminado'.");

        // V5: ningún insumo puede ser igual al producto output (sin auto-referencia)
        if (request.Lines.Any(l => l.IdProductInput == request.IdProductOutput))
            throw new InvalidOperationException(
                "Un insumo de la receta no puede ser el mismo producto que el output.");

        var recipe = new ProductRecipe
        {
            IdProductOutput   = request.IdProductOutput,
            NameRecipe        = request.NameRecipe,
            QuantityOutput    = request.QuantityOutput,
            DescriptionRecipe = request.DescriptionRecipe,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            ProductRecipeLines = request.Lines.Select(l => new ProductRecipeLine
            {
                IdProductInput = l.IdProductInput,
                QuantityInput  = l.QuantityInput,
                SortOrder      = l.SortOrder
            }).ToList()
        };

        db.ProductRecipe.Add(recipe);
        await db.SaveChangesAsync(ct);

        var created = await WithIncludes(db.ProductRecipe.AsNoTracking())
            .FirstAsync(r => r.IdProductRecipe == recipe.IdProductRecipe, ct);

        return ToResponse(created);
    }

    public async Task<ProductRecipeResponse?> UpdateAsync(int idProductRecipe, UpdateProductRecipeRequest request, CancellationToken ct = default)
    {
        var recipe = await db.ProductRecipe
            .Include(r => r.ProductRecipeLines)
            .FirstOrDefaultAsync(r => r.IdProductRecipe == idProductRecipe, ct);

        if (recipe is null) return null;

        // V4: el producto output no puede ser Materia Prima (Id=1) ni Reventa (Id=4)
        var outputProduct = await db.Product.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProduct == recipe.IdProductOutput, ct);
        if (outputProduct?.IdProductType is 1 or 4)
            throw new InvalidOperationException(
                "El producto output no puede ser de tipo 'Materia Prima' o 'Reventa'.");

        // V5: ningún insumo puede ser igual al producto output
        if (request.Lines.Any(l => l.IdProductInput == recipe.IdProductOutput))
            throw new InvalidOperationException(
                "Un insumo de la receta no puede ser el mismo producto que el output.");

        recipe.NameRecipe        = request.NameRecipe;
        recipe.QuantityOutput    = request.QuantityOutput;
        recipe.DescriptionRecipe = request.DescriptionRecipe;
        recipe.IsActive          = request.IsActive;

        recipe.ProductRecipeLines.Clear();
        foreach (var l in request.Lines)
        {
            recipe.ProductRecipeLines.Add(new ProductRecipeLine
            {
                IdProductInput = l.IdProductInput,
                QuantityInput  = l.QuantityInput,
                SortOrder      = l.SortOrder
            });
        }

        await db.SaveChangesAsync(ct);

        var updated = await WithIncludes(db.ProductRecipe.AsNoTracking())
            .FirstAsync(r => r.IdProductRecipe == idProductRecipe, ct);

        return ToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int idProductRecipe, CancellationToken ct = default)
    {
        var deleted = await db.ProductRecipe
            .Where(r => r.IdProductRecipe == idProductRecipe)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
