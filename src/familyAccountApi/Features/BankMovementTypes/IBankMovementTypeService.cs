using FamilyAccountApi.Features.BankMovementTypes.Dtos;

namespace FamilyAccountApi.Features.BankMovementTypes;

public interface IBankMovementTypeService
{
    Task<IReadOnlyList<BankMovementTypeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BankMovementTypeResponse?> GetByIdAsync(int idBankMovementType, CancellationToken ct = default);
    Task<BankMovementTypeResponse> CreateAsync(CreateBankMovementTypeRequest request, CancellationToken ct = default);
    Task<BankMovementTypeResponse?> UpdateAsync(int idBankMovementType, UpdateBankMovementTypeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBankMovementType, CancellationToken ct = default);
}
