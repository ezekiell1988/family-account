using FamilyAccountApi.Features.BankAccounts.Dtos;

namespace FamilyAccountApi.Features.BankAccounts;

public interface IBankAccountService
{
    Task<IReadOnlyList<BankAccountResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BankAccountResponse?> GetByIdAsync(int idBankAccount, CancellationToken ct = default);
    Task<BankAccountResponse> CreateAsync(CreateBankAccountRequest request, CancellationToken ct = default);
    Task<BankAccountResponse?> UpdateAsync(int idBankAccount, UpdateBankAccountRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBankAccount, CancellationToken ct = default);
}