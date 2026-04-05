using FamilyAccountApi.Features.Products.Dtos;

namespace FamilyAccountApi.Features.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ProductResponse?> GetByIdAsync(int idProduct, CancellationToken ct = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductResponse?> UpdateAsync(int idProduct, UpdateProductRequest request, CancellationToken ct = default);
    Task<(bool Deleted, string? ConflictMessage)> DeleteAsync(int idProduct, CancellationToken ct = default);
    /// <summary>
    /// Devuelve los productos con ReorderPoint configurado cuyo stock total esté por debajo del umbral.
    /// </summary>
    Task<IReadOnlyList<ProductResponse>> GetBelowReorderPointAsync(CancellationToken ct = default);
}
