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
        pu.BrandPresentation);

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
                pu.CodeBarcode, pu.NamePresentation, pu.BrandPresentation))
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
            BrandPresentation = request.BrandPresentation
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

        entity.ConversionFactor  = request.ConversionFactor;
        entity.IsBase            = request.IsBase;
        entity.UsedForPurchase   = request.UsedForPurchase;
        entity.UsedForSale       = request.UsedForSale;
        entity.CodeBarcode       = request.CodeBarcode;
        entity.NamePresentation  = request.NamePresentation;
        entity.BrandPresentation = request.BrandPresentation;

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
