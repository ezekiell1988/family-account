using FamilyAccountApi.Features.ProductAttributes.Dtos;

namespace FamilyAccountApi.Features.ProductAttributes;

public interface IProductAttributeService
{
    Task<IReadOnlyList<ProductAttributeResponse>> GetAllByProductAsync(int idProduct, CancellationToken ct = default);

    Task<(ProductAttributeResponse? Attribute, string? Error)> CreateAttributeAsync(
        int idProduct, CreateProductAttributeRequest request, CancellationToken ct = default);

    Task<(ProductAttributeResponse? Attribute, string? Error)> UpdateAttributeAsync(
        int idProduct, int idProductAttribute, UpdateProductAttributeRequest request, CancellationToken ct = default);

    Task<(bool Deleted, string? ConflictMessage)> DeleteAttributeAsync(
        int idProduct, int idProductAttribute, CancellationToken ct = default);

    Task<(AttributeValueResponse? Value, string? Error)> CreateValueAsync(
        int idProduct, int idProductAttribute, CreateAttributeValueRequest request, CancellationToken ct = default);

    Task<(AttributeValueResponse? Value, string? Error)> UpdateValueAsync(
        int idProduct, int idProductAttribute, int idAttributeValue, UpdateAttributeValueRequest request, CancellationToken ct = default);

    Task<bool> DeleteValueAsync(
        int idProduct, int idProductAttribute, int idAttributeValue, CancellationToken ct = default);
}
