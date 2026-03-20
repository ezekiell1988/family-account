using FamilyAccountApi.Features.AccountingEntries.Dtos;

namespace FamilyAccountApi.Features.AccountingEntries;

public interface IAccountingEntryService
{
    Task<IReadOnlyList<AccountingEntryResponse>> GetAllAsync(CancellationToken ct = default);
    Task<AccountingEntryResponse?> GetByIdAsync(int idAccountingEntry, CancellationToken ct = default);
    Task<AccountingEntryResponse> CreateAsync(CreateAccountingEntryRequest request, CancellationToken ct = default);
    Task<AccountingEntryResponse?> UpdateAsync(int idAccountingEntry, UpdateAccountingEntryRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idAccountingEntry, CancellationToken ct = default);
}
