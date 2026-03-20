using FamilyAccountApi.Features.Banks.Dtos;

namespace FamilyAccountApi.Features.Banks;

public interface IBankService
{
    Task<IReadOnlyList<BankResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BankResponse?> GetByIdAsync(int idBank, CancellationToken ct = default);
    Task<BankResponse> CreateAsync(CreateBankRequest request, CancellationToken ct = default);
    Task<BankResponse?> UpdateAsync(int idBank, UpdateBankRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBank, CancellationToken ct = default);
}
