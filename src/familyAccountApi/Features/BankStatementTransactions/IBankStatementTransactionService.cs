using FamilyAccountApi.Features.BankStatementTransactions.Dtos;

namespace FamilyAccountApi.Features.BankStatementTransactions;

public interface IBankStatementTransactionService
{
    Task<IReadOnlyList<BankStatementTransactionResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BankStatementTransactionResponse>> GetByImportIdAsync(int idBankStatementImport, CancellationToken ct = default);
    Task<BankStatementTransactionResponse?> GetByIdAsync(int idBankStatementTransaction, CancellationToken ct = default);
    Task<BankStatementTransactionResponse> CreateAsync(CreateBankStatementTransactionRequest request, CancellationToken ct = default);
    Task<BankStatementTransactionResponse?> UpdateAsync(int idBankStatementTransaction, UpdateBankStatementTransactionRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBankStatementTransaction, CancellationToken ct = default);
}
