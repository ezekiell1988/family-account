using FamilyAccountApi.Features.ProductTypes.Dtos;

namespace FamilyAccountApi.Features.ProductTypes;

public interface IProductTypeService
{
    Task<IReadOnlyList<ProductTypeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ProductTypeResponse?> GetByIdAsync(int idProductType, CancellationToken ct = default);
    Task<ProductTypeResponse> CreateAsync(CreateProductTypeRequest request, CancellationToken ct = default);
    Task<ProductTypeResponse?> UpdateAsync(int idProductType, UpdateProductTypeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProductType, CancellationToken ct = default);
}
