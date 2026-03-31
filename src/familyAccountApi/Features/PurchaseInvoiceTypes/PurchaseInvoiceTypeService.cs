using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.PurchaseInvoiceTypes.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.PurchaseInvoiceTypes;

public sealed class PurchaseInvoiceTypeService(AppDbContext db) : IPurchaseInvoiceTypeService
{
    private IQueryable<PurchaseInvoiceType> WithIncludes() =>
        db.PurchaseInvoiceType.AsNoTracking()
            .Include(pit => pit.IdAccountCounterpartCRCNavigation)
            .Include(pit => pit.IdAccountCounterpartUSDNavigation)
            .Include(pit => pit.IdDefaultExpenseAccountNavigation);

    private static PurchaseInvoiceTypeResponse ToResponse(PurchaseInvoiceType pit) => new(
        pit.IdPurchaseInvoiceType,
        pit.CodePurchaseInvoiceType,
        pit.NamePurchaseInvoiceType,
        pit.CounterpartFromBankMovement,
        pit.IdAccountCounterpartCRC,
        pit.IdAccountCounterpartCRCNavigation?.CodeAccount,
        pit.IdAccountCounterpartCRCNavigation?.NameAccount,
        pit.IdAccountCounterpartUSD,
        pit.IdAccountCounterpartUSDNavigation?.CodeAccount,
        pit.IdAccountCounterpartUSDNavigation?.NameAccount,
        pit.IdDefaultExpenseAccount,
        pit.IdDefaultExpenseAccountNavigation?.CodeAccount,
        pit.IdDefaultExpenseAccountNavigation?.NameAccount,
        pit.IsActive);

    public async Task<IReadOnlyList<PurchaseInvoiceTypeResponse>> GetAllAsync(CancellationToken ct = default) =>
        (await WithIncludes().ToListAsync(ct)).Select(ToResponse).ToList();

    public async Task<IReadOnlyList<PurchaseInvoiceTypeResponse>> GetActiveAsync(CancellationToken ct = default) =>
        (await WithIncludes().Where(pit => pit.IsActive).ToListAsync(ct)).Select(ToResponse).ToList();

    public async Task<PurchaseInvoiceTypeResponse?> GetByIdAsync(int idPurchaseInvoiceType, CancellationToken ct = default)
    {
        var entity = await WithIncludes()
            .FirstOrDefaultAsync(pit => pit.IdPurchaseInvoiceType == idPurchaseInvoiceType, ct);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<PurchaseInvoiceTypeResponse> CreateAsync(CreatePurchaseInvoiceTypeRequest request, CancellationToken ct = default)
    {
        var entity = new PurchaseInvoiceType
        {
            CodePurchaseInvoiceType     = request.CodePurchaseInvoiceType,
            NamePurchaseInvoiceType     = request.NamePurchaseInvoiceType,
            CounterpartFromBankMovement = request.CounterpartFromBankMovement,
            IdAccountCounterpartCRC     = request.IdAccountCounterpartCRC,
            IdAccountCounterpartUSD     = request.IdAccountCounterpartUSD,
            IdDefaultExpenseAccount     = request.IdDefaultExpenseAccount,
            IsActive                    = request.IsActive
        };

        db.PurchaseInvoiceType.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.IdPurchaseInvoiceType, ct))!;
    }

    public async Task<PurchaseInvoiceTypeResponse?> UpdateAsync(int idPurchaseInvoiceType, UpdatePurchaseInvoiceTypeRequest request, CancellationToken ct = default)
    {
        var entity = await db.PurchaseInvoiceType.FindAsync([idPurchaseInvoiceType], ct);
        if (entity is null) return null;

        entity.NamePurchaseInvoiceType     = request.NamePurchaseInvoiceType;
        entity.CounterpartFromBankMovement = request.CounterpartFromBankMovement;
        entity.IdAccountCounterpartCRC     = request.IdAccountCounterpartCRC;
        entity.IdAccountCounterpartUSD     = request.IdAccountCounterpartUSD;
        entity.IdDefaultExpenseAccount     = request.IdDefaultExpenseAccount;
        entity.IsActive                    = request.IsActive;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(idPurchaseInvoiceType, ct);
    }

    public async Task<bool> DeleteAsync(int idPurchaseInvoiceType, CancellationToken ct = default)
    {
        var deleted = await db.PurchaseInvoiceType
            .Where(pit => pit.IdPurchaseInvoiceType == idPurchaseInvoiceType)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
