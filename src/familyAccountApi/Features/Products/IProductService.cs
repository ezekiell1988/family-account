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
    /// <summary>
    /// Retorna los productos hijos (variantes) de un producto padre con sus atributos expandidos.
    /// </summary>
    Task<IReadOnlyList<VariantSummary>> GetVariantsAsync(int idProductParent, CancellationToken ct = default);
    /// <summary>
    /// Genera el producto cartesiano de los atributos del padre y crea variantes hijas.
    /// Omite combinaciones que ya existen.
    /// </summary>
    Task<(GenerateVariantsResponse? Result, string? Error)> GenerateVariantsAsync(
        int idProductParent, GenerateVariantsRequest request, CancellationToken ct = default);
}
