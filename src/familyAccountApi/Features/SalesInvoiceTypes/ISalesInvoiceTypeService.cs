using FamilyAccountApi.Features.SalesInvoiceTypes.Dtos;

namespace FamilyAccountApi.Features.SalesInvoiceTypes;

public interface ISalesInvoiceTypeService
{
    Task<IReadOnlyList<SalesInvoiceTypeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SalesInvoiceTypeResponse>> GetActiveAsync(CancellationToken ct = default);
    Task<SalesInvoiceTypeResponse?> GetByIdAsync(int idSalesInvoiceType, CancellationToken ct = default);
    Task<SalesInvoiceTypeResponse> CreateAsync(CreateSalesInvoiceTypeRequest request, CancellationToken ct = default);
    Task<SalesInvoiceTypeResponse?> UpdateAsync(int idSalesInvoiceType, UpdateSalesInvoiceTypeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idSalesInvoiceType, CancellationToken ct = default);
}
