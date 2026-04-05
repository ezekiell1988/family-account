using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductComboSlots.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductComboSlots;

public sealed class ProductComboSlotService(AppDbContext db) : IProductComboSlotService
{
    private static ProductComboSlotResponse ToResponse(ProductComboSlot s) => new(
        s.IdProductComboSlot,
        s.IdProductCombo,
        s.NameSlot,
        s.Quantity,
        s.IsRequired,
        s.SortOrder,
        s.ProductComboSlotProducts
            .OrderBy(sp => sp.SortOrder)
            .Select(sp => new ProductComboSlotProductResponse(
                sp.IdProductComboSlotProduct,
                sp.IdProduct,
                sp.IdProductNavigation.NameProduct,
                sp.PriceAdjustment,
                sp.SortOrder))
            .ToList(),
        s.PresetOptions
            .Select(po => new ProductComboSlotPresetOptionResponse(
                po.IdProductComboSlotPresetOption,
                po.IdProductOptionItem,
                po.IdProductOptionItemNavigation.NameItem,
                po.IdProductOptionItemNavigation.PriceDelta))
            .ToList());

    private static IQueryable<ProductComboSlot> WithIncludes(IQueryable<ProductComboSlot> q)
        => q.Include(s => s.ProductComboSlotProducts)
               .ThenInclude(sp => sp.IdProductNavigation)
             .Include(s => s.PresetOptions)
               .ThenInclude(po => po.IdProductOptionItemNavigation);

    public async Task<IReadOnlyList<ProductComboSlotResponse>> GetByComboAsync(int idProductCombo, CancellationToken ct = default)
    {
        var list = await WithIncludes(db.ProductComboSlot.AsNoTracking())
            .Where(s => s.IdProductCombo == idProductCombo)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<ProductComboSlotResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var slot = await WithIncludes(db.ProductComboSlot.AsNoTracking())
            .FirstOrDefaultAsync(s => s.IdProductComboSlot == id, ct);

        return slot is null ? null : ToResponse(slot);
    }

    public async Task<(ProductComboSlotResponse result, string? error)> CreateAsync(
        CreateProductComboSlotRequest request, CancellationToken ct = default)
    {
        // V6: solo crear en productos con IsCombo = true
        var product = await db.Product.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProduct == request.IdProductCombo, ct);

        if (product is null)
            return (null!, "Producto combo no encontrado.");

        if (!product.IsCombo)
            return (null!, "El producto no tiene IsCombo habilitado.");

        // V7: al menos 1 producto permitido
        if (request.Products.Count == 0)
            return (null!, "El slot debe tener al menos un producto permitido.");

        // V8: no repetir el mismo IdProduct en el mismo slot
        var productIds = request.Products.Select(p => p.IdProduct).ToList();
        if (productIds.Distinct().Count() != productIds.Count)
            return (null!, "No se puede repetir el mismo producto en el mismo slot.");

        // V9: no se permiten combos anidados
        var combosInSlot = await db.Product.AsNoTracking()
            .Where(p => productIds.Contains(p.IdProduct) && p.IsCombo)
            .Select(p => p.NameProduct)
            .ToListAsync(ct);
        if (combosInSlot.Count > 0)
            return (null!, $"Los siguientes productos son combos y no pueden asignarse a un slot: {string.Join(", ", combosInSlot)}.");

        var slot = new ProductComboSlot
        {
            IdProductCombo = request.IdProductCombo,
            NameSlot       = request.NameSlot,
            Quantity       = request.Quantity,
            IsRequired     = request.IsRequired,
            SortOrder      = request.SortOrder,
            ProductComboSlotProducts = request.Products.Select(p => new ProductComboSlotProduct
            {
                IdProduct       = p.IdProduct,
                PriceAdjustment = p.PriceAdjustment,
                SortOrder       = p.SortOrder
            }).ToList()
        };

        db.ProductComboSlot.Add(slot);
        await db.SaveChangesAsync(ct);

        var created = await WithIncludes(db.ProductComboSlot.AsNoTracking())
            .FirstAsync(s => s.IdProductComboSlot == slot.IdProductComboSlot, ct);

        return (ToResponse(created), null);
    }

    public async Task<(ProductComboSlotResponse? result, string? error)> UpdateAsync(
        int id, UpdateProductComboSlotRequest request, CancellationToken ct = default)
    {
        var slot = await db.ProductComboSlot
            .Include(s => s.ProductComboSlotProducts)
            .FirstOrDefaultAsync(s => s.IdProductComboSlot == id, ct);

        if (slot is null) return (null, null);

        // V7
        if (request.Products.Count == 0)
            return (null!, "El slot debe tener al menos un producto permitido.");

        // V8
        var productIds = request.Products.Select(p => p.IdProduct).ToList();
        if (productIds.Distinct().Count() != productIds.Count)
            return (null!, "No se puede repetir el mismo producto en el mismo slot.");

        // V9: no se permiten combos anidados
        var combosInSlot = await db.Product.AsNoTracking()
            .Where(p => productIds.Contains(p.IdProduct) && p.IsCombo)
            .Select(p => p.NameProduct)
            .ToListAsync(ct);
        if (combosInSlot.Count > 0)
            return (null!, $"Los siguientes productos son combos y no pueden asignarse a un slot: {string.Join(", ", combosInSlot)}.");

        slot.NameSlot   = request.NameSlot;
        slot.Quantity   = request.Quantity;
        slot.IsRequired = request.IsRequired;
        slot.SortOrder  = request.SortOrder;

        // Reemplazar productos permitidos
        db.ProductComboSlotProduct.RemoveRange(slot.ProductComboSlotProducts);
        slot.ProductComboSlotProducts = request.Products.Select(p => new ProductComboSlotProduct
        {
            IdProductComboSlot = slot.IdProductComboSlot,
            IdProduct          = p.IdProduct,
            PriceAdjustment    = p.PriceAdjustment,
            SortOrder          = p.SortOrder
        }).ToList();

        await db.SaveChangesAsync(ct);

        var updated = await WithIncludes(db.ProductComboSlot.AsNoTracking())
            .FirstAsync(s => s.IdProductComboSlot == id, ct);

        return (ToResponse(updated), null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var slot = await db.ProductComboSlot.FindAsync([id], ct);
        if (slot is null) return false;

        db.ProductComboSlot.Remove(slot);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Preset options
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<(ProductComboSlotPresetOptionResponse? result, string? error)> CreatePresetOptionAsync(
        int slotId, CreateProductComboSlotPresetOptionRequest request, CancellationToken ct = default)
    {
        // Cargar slot con productos permitidos
        var slot = await db.ProductComboSlot
            .Include(s => s.ProductComboSlotProducts)
            .FirstOrDefaultAsync(s => s.IdProductComboSlot == slotId, ct);

        if (slot is null)
            return (null, null);  // 404

        // V-PRESET-1: el item debe pertenecer a algún producto permitido en el slot
        var allowedProductIds = slot.ProductComboSlotProducts.Select(p => p.IdProduct).ToList();
        var optionItem = await db.ProductOptionItem
            .AsNoTracking()
            .Include(oi => oi.IdProductOptionGroupNavigation)
            .FirstOrDefaultAsync(oi => oi.IdProductOptionItem == request.IdProductOptionItem, ct);

        if (optionItem is null)
            return (null!, "Ítem de opción no encontrado.");

        if (!allowedProductIds.Contains(optionItem.IdProductOptionGroupNavigation.IdProduct))
            return (null!, "V-PRESET-1: el ítem de opción no pertenece a ningún producto permitido en este slot.");

        // V-PRESET-2: sin duplicado por slot
        var exists = await db.ProductComboSlotPresetOption.AsNoTracking()
            .AnyAsync(p => p.IdProductComboSlot == slotId
                        && p.IdProductOptionItem == request.IdProductOptionItem, ct);

        if (exists)
            return (null!, "V-PRESET-2: el ítem de opción ya está registrado como preset en este slot.");

        var preset = new ProductComboSlotPresetOption
        {
            IdProductComboSlot  = slotId,
            IdProductOptionItem = request.IdProductOptionItem
        };

        db.ProductComboSlotPresetOption.Add(preset);
        await db.SaveChangesAsync(ct);

        return (new ProductComboSlotPresetOptionResponse(
            preset.IdProductComboSlotPresetOption,
            optionItem.IdProductOptionItem,
            optionItem.NameItem,
            optionItem.PriceDelta), null);
    }

    public async Task<bool> DeletePresetOptionAsync(int slotId, int presetOptionId, CancellationToken ct = default)
    {
        var preset = await db.ProductComboSlotPresetOption
            .FirstOrDefaultAsync(p => p.IdProductComboSlotPresetOption == presetOptionId
                                   && p.IdProductComboSlot == slotId, ct);

        if (preset is null) return false;

        db.ProductComboSlotPresetOption.Remove(preset);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
