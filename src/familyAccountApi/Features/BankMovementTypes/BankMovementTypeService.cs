using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankMovementTypes.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankMovementTypes;

public sealed class BankMovementTypeService(AppDbContext db) : IBankMovementTypeService
{
    public async Task<IReadOnlyList<BankMovementTypeResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await BuildQuery()
            .OrderBy(bmt => bmt.CodeBankMovementType)
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<BankMovementTypeResponse?> GetByIdAsync(int idBankMovementType, CancellationToken ct = default)
    {
        return await BuildQuery()
            .Where(bmt => bmt.IdBankMovementType == idBankMovementType)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BankMovementTypeResponse> CreateAsync(CreateBankMovementTypeRequest request, CancellationToken ct = default)
    {
        await ValidateAccountAsync(request.IdAccountCounterpart, ct);

        var entity = new BankMovementType
        {
            CodeBankMovementType = request.CodeBankMovementType.Trim().ToUpper(),
            NameBankMovementType = request.NameBankMovementType.Trim(),
            IdAccountCounterpart = request.IdAccountCounterpart,
            MovementSign         = request.MovementSign.Trim(),
            IsActive             = request.IsActive
        };

        db.BankMovementType.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.IdBankMovementType, ct))!;
    }

    public async Task<BankMovementTypeResponse?> UpdateAsync(int idBankMovementType, UpdateBankMovementTypeRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankMovementType.FindAsync([idBankMovementType], ct);
        if (entity is null) return null;

        await ValidateAccountAsync(request.IdAccountCounterpart, ct);

        entity.CodeBankMovementType = request.CodeBankMovementType.Trim().ToUpper();
        entity.NameBankMovementType = request.NameBankMovementType.Trim();
        entity.IdAccountCounterpart = request.IdAccountCounterpart;
        entity.MovementSign         = request.MovementSign.Trim();
        entity.IsActive             = request.IsActive;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(idBankMovementType, ct);
    }

    public async Task<bool> DeleteAsync(int idBankMovementType, CancellationToken ct = default)
    {
        var deleted = await db.BankMovementType
            .Where(bmt => bmt.IdBankMovementType == idBankMovementType)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    private IQueryable<BankMovementType> BuildQuery()
    {
        return db.BankMovementType
            .AsNoTracking()
            .Include(bmt => bmt.IdAccountCounterpartNavigation);
    }

    private static System.Linq.Expressions.Expression<Func<BankMovementType, BankMovementTypeResponse>> MapResponse()
    {
        return bmt => new BankMovementTypeResponse(
            bmt.IdBankMovementType,
            bmt.CodeBankMovementType,
            bmt.NameBankMovementType,
            bmt.IdAccountCounterpart,
            bmt.IdAccountCounterpartNavigation.CodeAccount,
            bmt.IdAccountCounterpartNavigation.NameAccount,
            bmt.MovementSign,
            bmt.IsActive);
    }

    private async Task ValidateAccountAsync(int idAccount, CancellationToken ct)
    {
        var exists = await db.Account
            .AsNoTracking()
            .AnyAsync(a => a.IdAccount == idAccount && a.AllowsMovements, ct);

        if (!exists)
            throw new InvalidOperationException($"La cuenta contable con ID {idAccount} no existe o no permite movimientos.");
    }
}
