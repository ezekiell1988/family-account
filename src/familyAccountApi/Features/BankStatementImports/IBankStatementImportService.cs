using FamilyAccountApi.Features.BankStatementImports.Dtos;

namespace FamilyAccountApi.Features.BankStatementImports;

public interface IBankStatementImportService
{
    Task<IReadOnlyList<BankStatementImportResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BankStatementImportResponse?> GetByIdAsync(int idBankStatementImport, CancellationToken ct = default);
    Task<BankStatementImportResponse> CreateAsync(CreateBankStatementImportRequest request, int importedBy, CancellationToken ct = default);
    Task<BankStatementImportResponse?> UpdateAsync(int idBankStatementImport, UpdateBankStatementImportRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBankStatementImport, CancellationToken ct = default);
}
