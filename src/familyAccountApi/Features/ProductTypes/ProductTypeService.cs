using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductTypes.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductTypes;

public sealed class ProductTypeService(AppDbContext db) : IProductTypeService
{
    public async Task<IReadOnlyList<ProductTypeResponse>> GetAllAsync(CancellationToken ct = default)
        => await db.ProductType
            .AsNoTracking()
            .OrderBy(pt => pt.NameProductType)
            .Select(pt => new ProductTypeResponse(pt.IdProductType, pt.NameProductType, pt.DescriptionProductType))
            .ToListAsync(ct);

    public async Task<ProductTypeResponse?> GetByIdAsync(int idProductType, CancellationToken ct = default)
        => await db.ProductType
            .AsNoTracking()
            .Where(pt => pt.IdProductType == idProductType)
            .Select(pt => new ProductTypeResponse(pt.IdProductType, pt.NameProductType, pt.DescriptionProductType))
            .FirstOrDefaultAsync(ct);

    public async Task<ProductTypeResponse> CreateAsync(CreateProductTypeRequest request, CancellationToken ct = default)
    {
        var entity = new ProductType
        {
            NameProductType        = request.NameProductType,
            DescriptionProductType = request.DescriptionProductType
        };

        db.ProductType.Add(entity);
        await db.SaveChangesAsync(ct);

        return new ProductTypeResponse(entity.IdProductType, entity.NameProductType, entity.DescriptionProductType);
    }

    public async Task<ProductTypeResponse?> UpdateAsync(int idProductType, UpdateProductTypeRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductType.FindAsync([idProductType], ct);
        if (entity is null) return null;

        entity.NameProductType        = request.NameProductType;
        entity.DescriptionProductType = request.DescriptionProductType;

        await db.SaveChangesAsync(ct);

        return new ProductTypeResponse(entity.IdProductType, entity.NameProductType, entity.DescriptionProductType);
    }

    public async Task<bool> DeleteAsync(int idProductType, CancellationToken ct = default)
    {
        var deleted = await db.ProductType
            .Where(pt => pt.IdProductType == idProductType)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
