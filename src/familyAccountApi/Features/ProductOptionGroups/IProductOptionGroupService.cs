using FamilyAccountApi.Features.ProductOptionGroups.Dtos;

namespace FamilyAccountApi.Features.ProductOptionGroups;

public interface IProductOptionGroupService
{
    Task<IReadOnlyList<ProductOptionGroupResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default);
    Task<ProductOptionGroupResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(ProductOptionGroupResponse result, string? error)> CreateAsync(CreateProductOptionGroupRequest request, CancellationToken ct = default);
    Task<(ProductOptionGroupResponse? result, string? error)> UpdateAsync(int id, UpdateProductOptionGroupRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
