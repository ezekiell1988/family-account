using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.SalesInvoiceTypes.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.SalesInvoiceTypes;

public sealed class SalesInvoiceTypeService(AppDbContext db) : ISalesInvoiceTypeService
{
    private IQueryable<SalesInvoiceType> WithIncludes() =>
        db.SalesInvoiceType.AsNoTracking()
            .Include(sit => sit.IdAccountCounterpartCRCNavigation)
            .Include(sit => sit.IdAccountCounterpartUSDNavigation)
            .Include(sit => sit.IdAccountSalesRevenueNavigation)
            .Include(sit => sit.IdAccountCOGSNavigation)
            .Include(sit => sit.IdAccountInventoryNavigation);

    private static SalesInvoiceTypeResponse ToResponse(SalesInvoiceType sit) => new(
        sit.IdSalesInvoiceType,
        sit.CodeSalesInvoiceType,
        sit.NameSalesInvoiceType,
        sit.CounterpartFromBankMovement,
        sit.IdAccountCounterpartCRC,
        sit.IdAccountCounterpartCRCNavigation?.CodeAccount,
        sit.IdAccountCounterpartCRCNavigation?.NameAccount,
        sit.IdAccountCounterpartUSD,
        sit.IdAccountCounterpartUSDNavigation?.CodeAccount,
        sit.IdAccountCounterpartUSDNavigation?.NameAccount,
        sit.IdBankMovementType,
        sit.IdAccountSalesRevenue,
        sit.IdAccountSalesRevenueNavigation?.CodeAccount,
        sit.IdAccountSalesRevenueNavigation?.NameAccount,
        sit.IdAccountCOGS,
        sit.IdAccountCOGSNavigation?.CodeAccount,
        sit.IdAccountCOGSNavigation?.NameAccount,
        sit.IdAccountInventory,
        sit.IdAccountInventoryNavigation?.CodeAccount,
        sit.IdAccountInventoryNavigation?.NameAccount,
        sit.IsActive);

    public async Task<IReadOnlyList<SalesInvoiceTypeResponse>> GetAllAsync(CancellationToken ct = default) =>
        (await WithIncludes().ToListAsync(ct)).Select(ToResponse).ToList();

    public async Task<IReadOnlyList<SalesInvoiceTypeResponse>> GetActiveAsync(CancellationToken ct = default) =>
        (await WithIncludes().Where(sit => sit.IsActive).ToListAsync(ct)).Select(ToResponse).ToList();

    public async Task<SalesInvoiceTypeResponse?> GetByIdAsync(int idSalesInvoiceType, CancellationToken ct = default)
    {
        var entity = await WithIncludes()
            .FirstOrDefaultAsync(sit => sit.IdSalesInvoiceType == idSalesInvoiceType, ct);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<SalesInvoiceTypeResponse> CreateAsync(CreateSalesInvoiceTypeRequest request, CancellationToken ct = default)
    {
        var entity = new SalesInvoiceType
        {
            CodeSalesInvoiceType        = request.CodeSalesInvoiceType.Trim(),
            NameSalesInvoiceType        = request.NameSalesInvoiceType.Trim(),
            CounterpartFromBankMovement = request.CounterpartFromBankMovement,
            IdAccountCounterpartCRC     = request.IdAccountCounterpartCRC,
            IdAccountCounterpartUSD     = request.IdAccountCounterpartUSD,
            IdBankMovementType          = request.IdBankMovementType,
            IdAccountSalesRevenue       = request.IdAccountSalesRevenue,
            IdAccountCOGS              = request.IdAccountCOGS,
            IdAccountInventory          = request.IdAccountInventory,
            IsActive                    = request.IsActive
        };

        db.SalesInvoiceType.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.IdSalesInvoiceType, ct))!;
    }

    public async Task<SalesInvoiceTypeResponse?> UpdateAsync(int idSalesInvoiceType, UpdateSalesInvoiceTypeRequest request, CancellationToken ct = default)
    {
        var entity = await db.SalesInvoiceType.FindAsync([idSalesInvoiceType], ct);
        if (entity is null) return null;

        entity.NameSalesInvoiceType        = request.NameSalesInvoiceType.Trim();
        entity.CounterpartFromBankMovement = request.CounterpartFromBankMovement;
        entity.IdAccountCounterpartCRC     = request.IdAccountCounterpartCRC;
        entity.IdAccountCounterpartUSD     = request.IdAccountCounterpartUSD;
        entity.IdBankMovementType          = request.IdBankMovementType;
        entity.IdAccountSalesRevenue       = request.IdAccountSalesRevenue;
        entity.IdAccountCOGS              = request.IdAccountCOGS;
        entity.IdAccountInventory          = request.IdAccountInventory;
        entity.IsActive                    = request.IsActive;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(idSalesInvoiceType, ct);
    }

    public async Task<bool> DeleteAsync(int idSalesInvoiceType, CancellationToken ct = default)
    {
        var deleted = await db.SalesInvoiceType
            .Where(sit => sit.IdSalesInvoiceType == idSalesInvoiceType)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
