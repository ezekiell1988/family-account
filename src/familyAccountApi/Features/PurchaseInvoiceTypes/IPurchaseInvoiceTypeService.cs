using FamilyAccountApi.Features.PurchaseInvoiceTypes.Dtos;

namespace FamilyAccountApi.Features.PurchaseInvoiceTypes;

public interface IPurchaseInvoiceTypeService
{
    Task<IReadOnlyList<PurchaseInvoiceTypeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PurchaseInvoiceTypeResponse>> GetActiveAsync(CancellationToken ct = default);
    Task<PurchaseInvoiceTypeResponse?> GetByIdAsync(int idPurchaseInvoiceType, CancellationToken ct = default);
    Task<PurchaseInvoiceTypeResponse> CreateAsync(CreatePurchaseInvoiceTypeRequest request, CancellationToken ct = default);
    Task<PurchaseInvoiceTypeResponse?> UpdateAsync(int idPurchaseInvoiceType, UpdatePurchaseInvoiceTypeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idPurchaseInvoiceType, CancellationToken ct = default);
}
