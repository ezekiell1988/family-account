using FamilyAccountApi.Features.BankMovements.Dtos;
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
    /// <summary>Clasifica (o reclasifica) una transacción asignando tipo de movimiento y cuenta contrapartida.</summary>
    Task<BankStatementTransactionResponse?> ClassifyAsync(int idBankStatementTransaction, ClassifyBankStatementTransactionRequest request, CancellationToken ct = default);
    /// <summary>Crea un BankMovement a partir de una transacción clasificada y la marca como conciliada.</summary>
    Task<BankMovementResponse> CreateMovementFromTransactionAsync(int idBankStatementTransaction, CreateMovementFromTransactionRequest request, CancellationToken ct = default);
}
