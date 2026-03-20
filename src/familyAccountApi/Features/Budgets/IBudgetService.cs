using FamilyAccountApi.Features.Budgets.Dtos;

namespace FamilyAccountApi.Features.Budgets;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BudgetResponse?> GetByIdAsync(int idBudget, CancellationToken ct = default);
    Task<BudgetResponse> CreateAsync(CreateBudgetRequest request, CancellationToken ct = default);
    Task<BudgetResponse?> UpdateAsync(int idBudget, UpdateBudgetRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBudget, CancellationToken ct = default);
}