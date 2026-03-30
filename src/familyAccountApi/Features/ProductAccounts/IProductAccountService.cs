using FamilyAccountApi.Features.ProductAccounts.Dtos;

namespace FamilyAccountApi.Features.ProductAccounts;

public interface IProductAccountService
{
    Task<IReadOnlyList<ProductAccountResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductAccountResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default);
    Task<ProductAccountResponse?> GetByIdAsync(int idProductAccount, CancellationToken ct = default);
    Task<ProductAccountResponse> CreateAsync(CreateProductAccountRequest request, CancellationToken ct = default);
    Task<ProductAccountResponse?> UpdateAsync(int idProductAccount, UpdateProductAccountRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProductAccount, CancellationToken ct = default);
}
