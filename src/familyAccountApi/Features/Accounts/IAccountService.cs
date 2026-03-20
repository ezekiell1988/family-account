using FamilyAccountApi.Features.Accounts.Dtos;

namespace FamilyAccountApi.Features.Accounts;

public interface IAccountService
{
    Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken ct = default);
    Task<AccountResponse?> GetByIdAsync(int idAccount, CancellationToken ct = default);
    Task<IReadOnlyList<AccountResponse>> GetChildrenAsync(int idAccount, CancellationToken ct = default);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<AccountResponse?> UpdateAsync(int idAccount, UpdateAccountRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idAccount, CancellationToken ct = default);
}
