using FamilyAccountApi.Features.ProductCategories.Dtos;

namespace FamilyAccountApi.Features.ProductCategories;

public interface IProductCategoryService
{
    Task<IReadOnlyList<ProductCategoryResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ProductCategoryResponse?> GetByIdAsync(int idProductCategory, CancellationToken ct = default);
    Task<ProductCategoryResponse> CreateAsync(CreateProductCategoryRequest request, CancellationToken ct = default);
    Task<ProductCategoryResponse?> UpdateAsync(int idProductCategory, UpdateProductCategoryRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProductCategory, CancellationToken ct = default);
    Task<bool> AddToProductAsync(int idProduct, int idProductCategory, CancellationToken ct = default);
    Task<bool> RemoveFromProductAsync(int idProduct, int idProductCategory, CancellationToken ct = default);
}
