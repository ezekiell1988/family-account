using FamilyAccountApi.Features.BankStatementImports.Dtos;

namespace FamilyAccountApi.Features.BankStatementImports;

public interface IBankStatementImportService
{
    Task<IReadOnlyList<BankStatementImportResponse>> GetAllAsync(CancellationToken ct = default);
    Task<BankStatementImportResponse?> GetByIdAsync(int idBankStatementImport, CancellationToken ct = default);
    Task<BankStatementImportResponse> CreateAsync(CreateBankStatementImportRequest request, int importedBy, CancellationToken ct = default);
    Task<BankStatementImportResponse?> UpdateAsync(int idBankStatementImport, UpdateBankStatementImportRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idBankStatementImport, CancellationToken ct = default);

    /// <summary>
    /// Sube y procesa un archivo Excel (HTML-XLS) de extracto bancario,
    /// crea el registro de importación y todas las transacciones extraídas.
    /// </summary>
    Task<BankStatementImportResponse> UploadAsync(
        IFormFile file,
        int       idBankAccount,
        int       idBankStatementTemplate,
        int       importedBy,
        CancellationToken ct = default);
}
