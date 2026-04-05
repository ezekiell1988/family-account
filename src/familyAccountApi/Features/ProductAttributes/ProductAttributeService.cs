using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductAttributes.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductAttributes;

public sealed class ProductAttributeService(AppDbContext db) : IProductAttributeService
{
    private static ProductAttributeResponse ToAttributeResponse(ProductAttribute a) => new(
        a.IdProductAttribute,
        a.NameAttribute,
        a.SortOrder,
        a.AttributeValues
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.NameValue)
            .Select(v => new AttributeValueResponse(v.IdAttributeValue, v.NameValue, v.SortOrder))
            .ToList());

    private static AttributeValueResponse ToValueResponse(AttributeValue v) => new(
        v.IdAttributeValue,
        v.NameValue,
        v.SortOrder);

    public async Task<IReadOnlyList<ProductAttributeResponse>> GetAllByProductAsync(int idProduct, CancellationToken ct = default)
    {
        var attrs = await db.ProductAttribute
            .AsNoTracking()
            .Where(a => a.IdProduct == idProduct)
            .Include(a => a.AttributeValues)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.NameAttribute)
            .ToListAsync(ct);

        return attrs.Select(ToAttributeResponse).ToList();
    }

    public async Task<(ProductAttributeResponse? Attribute, string? Error)> CreateAttributeAsync(
        int idProduct, CreateProductAttributeRequest request, CancellationToken ct = default)
    {
        var productExists = await db.Product.AnyAsync(p => p.IdProduct == idProduct, ct);
        if (!productExists)
            return (null, "Producto no encontrado.");

        var attr = new ProductAttribute
        {
            IdProduct     = idProduct,
            NameAttribute = request.NameAttribute,
            SortOrder     = request.SortOrder
        };

        db.ProductAttribute.Add(attr);
        await db.SaveChangesAsync(ct);

        attr.AttributeValues = [];
        return (ToAttributeResponse(attr), null);
    }

    public async Task<(ProductAttributeResponse? Attribute, string? Error)> UpdateAttributeAsync(
        int idProduct, int idProductAttribute, UpdateProductAttributeRequest request, CancellationToken ct = default)
    {
        var attr = await db.ProductAttribute
            .Include(a => a.AttributeValues)
            .FirstOrDefaultAsync(a => a.IdProductAttribute == idProductAttribute && a.IdProduct == idProduct, ct);

        if (attr is null)
            return (null, null);

        attr.NameAttribute = request.NameAttribute;
        attr.SortOrder     = request.SortOrder;

        await db.SaveChangesAsync(ct);

        return (ToAttributeResponse(attr), null);
    }

    public async Task<(bool Deleted, string? ConflictMessage)> DeleteAttributeAsync(
        int idProduct, int idProductAttribute, CancellationToken ct = default)
    {
        var attr = await db.ProductAttribute
            .FirstOrDefaultAsync(a => a.IdProductAttribute == idProductAttribute && a.IdProduct == idProduct, ct);

        if (attr is null)
            return (false, null);

        var hasVariants = await db.ProductVariantAttribute
            .AnyAsync(va => va.IdAttributeValueNavigation.IdProductAttribute == idProductAttribute, ct);

        if (hasVariants)
            return (false, "No se puede eliminar el atributo porque hay variantes que lo utilizan. Elimine primero las variantes.");

        db.ProductAttribute.Remove(attr);
        await db.SaveChangesAsync(ct);

        return (true, null);
    }

    public async Task<(AttributeValueResponse? Value, string? Error)> CreateValueAsync(
        int idProduct, int idProductAttribute, CreateAttributeValueRequest request, CancellationToken ct = default)
    {
        var attrExists = await db.ProductAttribute
            .AnyAsync(a => a.IdProductAttribute == idProductAttribute && a.IdProduct == idProduct, ct);

        if (!attrExists)
            return (null, "Atributo no encontrado.");

        var value = new AttributeValue
        {
            IdProductAttribute = idProductAttribute,
            NameValue          = request.NameValue,
            SortOrder          = request.SortOrder
        };

        db.AttributeValue.Add(value);
        await db.SaveChangesAsync(ct);

        return (ToValueResponse(value), null);
    }

    public async Task<(AttributeValueResponse? Value, string? Error)> UpdateValueAsync(
        int idProduct, int idProductAttribute, int idAttributeValue, UpdateAttributeValueRequest request, CancellationToken ct = default)
    {
        var value = await db.AttributeValue
            .FirstOrDefaultAsync(v =>
                v.IdAttributeValue == idAttributeValue &&
                v.IdProductAttribute == idProductAttribute &&
                v.IdProductAttributeNavigation.IdProduct == idProduct, ct);

        if (value is null)
            return (null, null);

        value.NameValue  = request.NameValue;
        value.SortOrder  = request.SortOrder;

        await db.SaveChangesAsync(ct);

        return (ToValueResponse(value), null);
    }

    public async Task<bool> DeleteValueAsync(
        int idProduct, int idProductAttribute, int idAttributeValue, CancellationToken ct = default)
    {
        var deleted = await db.AttributeValue
            .Where(v =>
                v.IdAttributeValue == idAttributeValue &&
                v.IdProductAttribute == idProductAttribute &&
                v.IdProductAttributeNavigation.IdProduct == idProduct)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
