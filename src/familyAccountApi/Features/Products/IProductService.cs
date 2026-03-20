using FamilyAccountApi.Features.Products.Dtos;

namespace FamilyAccountApi.Features.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ProductResponse?> GetByIdAsync(int idProduct, CancellationToken ct = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductResponse?> UpdateAsync(int idProduct, UpdateProductRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProduct, CancellationToken ct = default);
    Task<bool> AddSKUAsync(int idProduct, int idProductSKU, CancellationToken ct = default);
    Task<bool> RemoveSKUAsync(int idProduct, int idProductSKU, CancellationToken ct = default);
}
