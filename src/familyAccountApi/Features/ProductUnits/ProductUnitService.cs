using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductUnits.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductUnits;

public sealed class ProductUnitService(AppDbContext db) : IProductUnitService
{
    private static ProductUnitResponse ToResponse(ProductUnit pu) => new(
        pu.IdProductUnit,
        pu.IdProduct,
        pu.IdUnit,
        pu.UnitOfMeasure.CodeUnit,
        pu.ConversionFactor,
        pu.IsBase,
        pu.UsedForPurchase,
        pu.UsedForSale,
        pu.CodeBarcode,
        pu.NamePresentation,
        pu.BrandPresentation,
        pu.SalePrice);

    public async Task<IReadOnlyList<ProductUnitResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default)
        => await db.ProductUnit
            .AsNoTracking()
            .Include(pu => pu.UnitOfMeasure)
            .Where(pu => pu.IdProduct == idProduct)
            .OrderByDescending(pu => pu.IsBase)
            .ThenBy(pu => pu.UnitOfMeasure.CodeUnit)
            .Select(pu => new ProductUnitResponse(
                pu.IdProductUnit, pu.IdProduct, pu.IdUnit, pu.UnitOfMeasure.CodeUnit,
                pu.ConversionFactor, pu.IsBase, pu.UsedForPurchase, pu.UsedForSale,
                pu.CodeBarcode, pu.NamePresentation, pu.BrandPresentation, pu.SalePrice))
            .ToListAsync(ct);

    public async Task<ProductUnitResponse?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var pu = await db.ProductUnit
            .AsNoTracking()
            .Include(pu => pu.UnitOfMeasure)
            .FirstOrDefaultAsync(pu => pu.CodeBarcode == barcode, ct);

        return pu is null ? null : ToResponse(pu);
    }

    public async Task<ProductUnitResponse?> GetByIdAsync(int idProductUnit, CancellationToken ct = default)
    {
        var pu = await db.ProductUnit
            .AsNoTracking()
            .Include(pu => pu.UnitOfMeasure)
            .FirstOrDefaultAsync(pu => pu.IdProductUnit == idProductUnit, ct);

        return pu is null ? null : ToResponse(pu);
    }

    public async Task<ProductUnitResponse> CreateAsync(CreateProductUnitRequest request, CancellationToken ct = default)
    {
        // V8: SalePrice no puede ser negativo
        if (request.SalePrice < 0)
            throw new InvalidOperationException("El precio de venta (SalePrice) no puede ser negativo.");

        if (request.IsBase)
        {
            // V2: solo puede existir 1 unidad base por producto
            var alreadyHasBase = await db.ProductUnit
                .AnyAsync(pu => pu.IdProduct == request.IdProduct && pu.IsBase, ct);
            if (alreadyHasBase)
                throw new InvalidOperationException(
                    "El producto ya tiene una presentación base (IsBase = true). Solo puede existir una unidad base por producto.");

            // V3: IsBase requiere ConversionFactor = 1 y IdUnit = Product.IdUnit
            if (request.ConversionFactor != 1m)
                throw new InvalidOperationException(
                    "La presentación base (IsBase = true) debe tener ConversionFactor = 1.");

            var product = await db.Product.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProduct == request.IdProduct, ct);
            if (product is not null && request.IdUnit != product.IdUnit)
                throw new InvalidOperationException(
                    "La unidad de la presentación base (IsBase = true) debe coincidir con la unidad base del producto (Product.IdUnit).");
        }
        else
        {
            // V3: ConversionFactor debe ser > 0 para presentaciones no-base
            if (request.ConversionFactor <= 0)
                throw new InvalidOperationException(
                    "El factor de conversión debe ser mayor que 0.");
        }

        var entity = new ProductUnit
        {
            IdProduct         = request.IdProduct,
            IdUnit            = request.IdUnit,
            ConversionFactor  = request.ConversionFactor,
            IsBase            = request.IsBase,
            UsedForPurchase   = request.UsedForPurchase,
            UsedForSale       = request.UsedForSale,
            CodeBarcode       = request.CodeBarcode,
            NamePresentation  = request.NamePresentation,
            BrandPresentation = request.BrandPresentation,
            SalePrice         = request.SalePrice
        };

        db.ProductUnit.Add(entity);
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(pu => pu.UnitOfMeasure).LoadAsync(ct);

        return ToResponse(entity);
    }

    public async Task<ProductUnitResponse?> UpdateAsync(int idProductUnit, UpdateProductUnitRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductUnit
            .Include(pu => pu.UnitOfMeasure)
            .FirstOrDefaultAsync(pu => pu.IdProductUnit == idProductUnit, ct);

        if (entity is null) return null;

        // V8: SalePrice no puede ser negativo
        if (request.SalePrice < 0)
            throw new InvalidOperationException("El precio de venta (SalePrice) no puede ser negativo.");

        if (request.IsBase)
        {
            // V2: verificar que no haya otra base para el mismo producto (excluir la fila actual)
            var alreadyHasBase = await db.ProductUnit
                .AnyAsync(pu => pu.IdProduct == entity.IdProduct && pu.IsBase && pu.IdProductUnit != idProductUnit, ct);
            if (alreadyHasBase)
                throw new InvalidOperationException(
                    "El producto ya tiene una presentación base (IsBase = true). Solo puede existir una unidad base por producto.");

            // V3: IsBase requiere ConversionFactor = 1 y la unidad no cambia (IdUnit del entity)
            if (request.ConversionFactor != 1m)
                throw new InvalidOperationException(
                    "La presentación base (IsBase = true) debe tener ConversionFactor = 1.");

            var product = await db.Product.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProduct == entity.IdProduct, ct);
            if (product is not null && entity.IdUnit != product.IdUnit)
                throw new InvalidOperationException(
                    "La unidad de esta presentación no coincide con la unidad base del producto. No se puede marcar como IsBase = true.");
        }
        else
        {
            if (request.ConversionFactor <= 0)
                throw new InvalidOperationException(
                    "El factor de conversión debe ser mayor que 0.");
        }

        entity.ConversionFactor  = request.ConversionFactor;
        entity.IsBase            = request.IsBase;
        entity.UsedForPurchase   = request.UsedForPurchase;
        entity.UsedForSale       = request.UsedForSale;
        entity.CodeBarcode       = request.CodeBarcode;
        entity.NamePresentation  = request.NamePresentation;
        entity.BrandPresentation = request.BrandPresentation;
        entity.SalePrice         = request.SalePrice;

        await db.SaveChangesAsync(ct);

        return ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int idProductUnit, CancellationToken ct = default)
    {
        var deleted = await db.ProductUnit
            .Where(pu => pu.IdProductUnit == idProductUnit)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
