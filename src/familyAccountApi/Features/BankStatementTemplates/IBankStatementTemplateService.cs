using FamilyAccountApi.Features.BankStatementTemplates.Dtos;

namespace FamilyAccountApi.Features.BankStatementTemplates;

public interface IBankStatementTemplateService
{
    Task<IReadOnlyList<BankStatementTemplateResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BankStatementTemplateResponse?> GetByIdAsync(int idBankStatementTemplate, CancellationToken ct = default);
    Task<BankStatementTemplateResponse> CreateAsync(CreateBankStatementTemplateRequest request, CancellationToken ct = default);
    Task<BankStatementTemplateResponse?> UpdateAsync(int idBankStatementTemplate, UpdateBankStatementTemplateRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBankStatementTemplate, CancellationToken ct = default);
}
