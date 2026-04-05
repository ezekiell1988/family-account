using FamilyAccountApi.Features.SalesInvoices.Dtos;

namespace FamilyAccountApi.Features.SalesInvoices;

public interface ISalesInvoiceService
{
    Task<IReadOnlyList<SalesInvoiceResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SalesInvoiceResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default);
    Task<SalesInvoiceResponse?> GetByIdAsync(int idSalesInvoice, CancellationToken ct = default);
    Task<SalesInvoiceResponse> CreateAsync(CreateSalesInvoiceRequest request, CancellationToken ct = default);
    Task<SalesInvoiceResponse?> UpdateAsync(int idSalesInvoice, UpdateSalesInvoiceRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idSalesInvoice, CancellationToken ct = default);
    Task<(bool Success, string? Error, SalesInvoiceResponse? Invoice)> ConfirmAsync(int idSalesInvoice, CancellationToken ct = default);
    Task<(SalesInvoiceResponse? Result, string? ConflictMessage)> CancelAsync(int idSalesInvoice, CancellationToken ct = default);
}
