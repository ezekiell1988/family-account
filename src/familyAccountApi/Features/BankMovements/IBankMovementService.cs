using FamilyAccountApi.Features.BankMovements.Dtos;

namespace FamilyAccountApi.Features.BankMovements;

public interface IBankMovementService
{
    Task<IReadOnlyList<BankMovementResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BankMovementResponse?> GetByIdAsync(int idBankMovement, CancellationToken ct = default);
    Task<BankMovementResponse> CreateAsync(CreateBankMovementRequest request, CancellationToken ct = default);
    Task<BankMovementResponse?> UpdateAsync(int idBankMovement, UpdateBankMovementRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBankMovement, CancellationToken ct = default);
    Task<BankMovementResponse?> ConfirmAsync(int idBankMovement, CancellationToken ct = default);
    Task<BankMovementResponse?> CancelAsync(int idBankMovement, CancellationToken ct = default);
}
