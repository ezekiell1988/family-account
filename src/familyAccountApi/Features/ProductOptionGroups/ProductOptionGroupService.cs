using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductOptionGroups.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductOptionGroups;

public sealed class ProductOptionGroupService(AppDbContext db) : IProductOptionGroupService
{
    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Verifica que todos los IdProductRecipe referenciados en los items existan en la BD.</summary>
    private async Task<string?> ValidateRecipesExistAsync(IEnumerable<ProductOptionItemRequest> items, CancellationToken ct)
    {
        var ids = items
            .Where(i => i.IdProductRecipe.HasValue)
            .Select(i => i.IdProductRecipe!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0) return null;

        var found = await db.ProductRecipe.AsNoTracking()
            .Where(r => ids.Contains(r.IdProductRecipe))
            .Select(r => r.IdProductRecipe)
            .ToListAsync(ct);

        var missing = ids.Except(found).ToList();
        return missing.Count > 0
            ? $"Las siguientes recetas no existen: {string.Join(", ", missing)}."
            : null;
    }

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
                i.SortOrder,
                i.IdProductRecipe))
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

        // V5: no more IsDefault=true items than MaxSelections
        var defaultCount = request.Items.Count(i => i.IsDefault);
        if (defaultCount > request.MaxSelections)
            return (null!, $"El número de ítems marcados como predeterminados ({defaultCount}) no puede ser mayor que MaxSelections ({request.MaxSelections}).");

        // V6: IdProductRecipe deben existir
        var recipeError = await ValidateRecipesExistAsync(request.Items, ct);
        if (recipeError is not null) return (null!, recipeError);

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
                NameItem        = i.NameItem,
                PriceDelta      = i.PriceDelta,
                IsDefault       = i.IsDefault,
                SortOrder       = i.SortOrder,
                IdProductRecipe = i.IdProductRecipe
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

        // V5: no more IsDefault=true items than MaxSelections
        var defaultCount = request.Items.Count(i => i.IsDefault);
        if (defaultCount > request.MaxSelections)
            return (null!, $"El número de ítems marcados como predeterminados ({defaultCount}) no puede ser mayor que MaxSelections ({request.MaxSelections}).");

        // V6: IdProductRecipe deben existir
        var recipeError = await ValidateRecipesExistAsync(request.Items, ct);
        if (recipeError is not null) return (null!, recipeError);

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
            SortOrder            = i.SortOrder,
            IdProductRecipe      = i.IdProductRecipe
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

    // ── Availability rules ───────────────────────────────────────────────────

    public async Task<(AvailabilityRuleResponse? Result, string? Error)> CreateAvailabilityRuleAsync(
        int idItem, CreateAvailabilityRuleRequest request, CancellationToken ct = default)
    {
        // V-DEP-4: sin auto-referencia
        if (request.IdRestrictedItem == request.IdEnablingItem)
            return (null, "El ítem restringido y el habilitador no pueden ser el mismo.");

        // Cargar ambos items para las validaciones
        var items = await db.ProductOptionItem
            .AsNoTracking()
            .Include(i => i.IdProductOptionGroupNavigation)
            .Where(i => i.IdProductOptionItem == request.IdRestrictedItem
                     || i.IdProductOptionItem == request.IdEnablingItem)
            .ToListAsync(ct);

        var restrictedItem = items.FirstOrDefault(i => i.IdProductOptionItem == request.IdRestrictedItem);
        var enablingItem   = items.FirstOrDefault(i => i.IdProductOptionItem == request.IdEnablingItem);

        if (restrictedItem is null) return (null, $"El ítem restringido {request.IdRestrictedItem} no existe.");
        if (enablingItem   is null) return (null, $"El ítem habilitador {request.IdEnablingItem} no existe.");

        // V-DEP-1: grupos distintos
        if (restrictedItem.IdProductOptionGroup == enablingItem.IdProductOptionGroup)
            return (null, "El ítem restringido y el habilitador deben pertenecer a grupos distintos.");

        // V-DEP-2: mismo producto
        if (restrictedItem.IdProductOptionGroupNavigation.IdProduct != enablingItem.IdProductOptionGroupNavigation.IdProduct)
            return (null, "El ítem restringido y el habilitador deben pertenecer al mismo producto.");

        // V-DEP-3: sin ciclos directos (si ya existe idEnablingItem restringido por idRestrictedItem)
        var cycleExists = await db.ProductOptionItemAvailability
            .AnyAsync(r => r.IdRestrictedItem == request.IdEnablingItem
                        && r.IdEnablingItem   == request.IdRestrictedItem, ct);
        if (cycleExists)
            return (null, "La regla crearía un ciclo directo entre los dos ítems.");

        // Duplicado
        var alreadyExists = await db.ProductOptionItemAvailability
            .AnyAsync(r => r.IdRestrictedItem == request.IdRestrictedItem
                        && r.IdEnablingItem   == request.IdEnablingItem, ct);
        if (alreadyExists)
            return (null, "La regla ya existe.");

        var rule = new ProductOptionItemAvailability
        {
            IdRestrictedItem = request.IdRestrictedItem,
            IdEnablingItem   = request.IdEnablingItem
        };

        db.ProductOptionItemAvailability.Add(rule);
        await db.SaveChangesAsync(ct);

        return (new AvailabilityRuleResponse(
            rule.IdProductOptionItemAvailability,
            rule.IdRestrictedItem,
            rule.IdEnablingItem,
            restrictedItem.NameItem,
            enablingItem.NameItem), null);
    }

    public async Task<bool> DeleteAvailabilityRuleAsync(int idRule, CancellationToken ct = default)
    {
        var deleted = await db.ProductOptionItemAvailability
            .Where(r => r.IdProductOptionItemAvailability == idRule)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public async Task<IReadOnlyList<AvailableGroupResponse>> GetAvailableGroupsAsync(
        int idProduct, IReadOnlyList<int> activeItemIds, CancellationToken ct = default)
    {
        // Cargar grupos + items + reglas del producto
        var groups = await db.ProductOptionGroup
            .AsNoTracking()
            .Include(g => g.ProductOptionItems)
                .ThenInclude(i => i.RestrictedByRules)
            .Where(g => g.IdProduct == idProduct)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(ct);

        var activeSet = activeItemIds.ToHashSet();

        return groups.Select(g => new AvailableGroupResponse(
            g.IdProductOptionGroup,
            g.NameGroup,
            g.IsRequired,
            g.MinSelections,
            g.MaxSelections,
            g.AllowSplit,
            g.SortOrder,
            g.ProductOptionItems.OrderBy(i => i.SortOrder).Select(i =>
            {
                // Un item es disponible si no tiene ninguna regla de restricción,
                // o si al menos uno de sus habilitadores está en activeSet.
                // Las reglas se cargan inline — para mejor performance en sets grandes
                // podría pasarse como parámetro, pero aquí el volumen es pequeño.
                return new AvailableItemResponse(
                    i.IdProductOptionItem,
                    i.NameItem,
                    i.PriceDelta,
                    i.IsDefault,
                    i.SortOrder,
                    i.IdProductRecipe,
                    IsAvailable: i.RestrictedByRules.Count == 0
                              || i.RestrictedByRules.Any(r => activeSet.Contains(r.IdEnablingItem)));
            }).ToList())).ToList();
    }
}
