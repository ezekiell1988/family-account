using FamilyAccountApi.Features.ProductSKUs.Dtos;

namespace FamilyAccountApi.Features.ProductSKUs;

public interface IProductSKUService
{
    Task<IReadOnlyList<ProductSKUResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ProductSKUResponse?> GetByIdAsync(int idProductSKU, CancellationToken ct = default);
    Task<ProductSKUResponse> CreateAsync(CreateProductSKURequest request, CancellationToken ct = default);
    Task<ProductSKUResponse?> UpdateAsync(int idProductSKU, UpdateProductSKURequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProductSKU, CancellationToken ct = default);
}
