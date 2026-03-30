using FamilyAccountApi.Features.PurchaseInvoices.Dtos;

namespace FamilyAccountApi.Features.PurchaseInvoices;

public interface IPurchaseInvoiceService
{
    Task<IReadOnlyList<PurchaseInvoiceResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PurchaseInvoiceResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default);
    Task<PurchaseInvoiceResponse?> GetByIdAsync(int idPurchaseInvoice, CancellationToken ct = default);
    Task<PurchaseInvoiceResponse> CreateAsync(CreatePurchaseInvoiceRequest request, CancellationToken ct = default);
    Task<PurchaseInvoiceResponse?> UpdateAsync(int idPurchaseInvoice, UpdatePurchaseInvoiceRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idPurchaseInvoice, CancellationToken ct = default);
    Task<(bool Success, string? Error, PurchaseInvoiceResponse? Invoice)> ConfirmAsync(int idPurchaseInvoice, CancellationToken ct = default);
    Task<PurchaseInvoiceResponse?> CancelAsync(int idPurchaseInvoice, CancellationToken ct = default);
}
