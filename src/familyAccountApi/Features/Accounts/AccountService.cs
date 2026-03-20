using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Accounts.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Accounts;

public sealed class AccountService(AppDbContext db) : IAccountService
{
    private static AccountResponse ToResponse(Account a) => new(
        a.IdAccount,
        a.CodeAccount,
        a.NameAccount,
        a.TypeAccount,
        a.LevelAccount,
        a.IdAccountParent,
        a.Parent?.NameAccount,
        a.AllowsMovements,
        a.IsActive);

    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Account
            .AsNoTracking()
            .Include(a => a.Parent)
            .OrderBy(a => a.CodeAccount)
            .Select(a => new AccountResponse(
                a.IdAccount,
                a.CodeAccount,
                a.NameAccount,
                a.TypeAccount,
                a.LevelAccount,
                a.IdAccountParent,
                a.Parent != null ? a.Parent.NameAccount : null,
                a.AllowsMovements,
                a.IsActive))
            .ToListAsync(ct);
    }

    public async Task<AccountResponse?> GetByIdAsync(int idAccount, CancellationToken ct = default)
    {
        var entity = await db.Account
            .AsNoTracking()
            .Include(a => a.Parent)
            .FirstOrDefaultAsync(a => a.IdAccount == idAccount, ct);

        return entity is null ? null : ToResponse(entity);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetChildrenAsync(int idAccount, CancellationToken ct = default)
    {
        return await db.Account
            .AsNoTracking()
            .Include(a => a.Parent)
            .Where(a => a.IdAccountParent == idAccount)
            .OrderBy(a => a.CodeAccount)
            .Select(a => new AccountResponse(
                a.IdAccount,
                a.CodeAccount,
                a.NameAccount,
                a.TypeAccount,
                a.LevelAccount,
                a.IdAccountParent,
                a.Parent != null ? a.Parent.NameAccount : null,
                a.AllowsMovements,
                a.IsActive))
            .ToListAsync(ct);
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        var entity = new Account
        {
            CodeAccount     = request.CodeAccount,
            NameAccount     = request.NameAccount,
            TypeAccount     = request.TypeAccount,
            LevelAccount    = request.LevelAccount,
            IdAccountParent = request.IdAccountParent,
            AllowsMovements = request.AllowsMovements,
            IsActive        = request.IsActive
        };

        db.Account.Add(entity);
        await db.SaveChangesAsync(ct);

        // Cargar padre si existe para devolver su nombre
        if (entity.IdAccountParent.HasValue)
            await db.Entry(entity).Reference(a => a.Parent).LoadAsync(ct);

        return ToResponse(entity);
    }

    public async Task<AccountResponse?> UpdateAsync(int idAccount, UpdateAccountRequest request, CancellationToken ct = default)
    {
        var entity = await db.Account.FindAsync([idAccount], ct);
        if (entity is null) return null;

        entity.CodeAccount     = request.CodeAccount;
        entity.NameAccount     = request.NameAccount;
        entity.TypeAccount     = request.TypeAccount;
        entity.LevelAccount    = request.LevelAccount;
        entity.IdAccountParent = request.IdAccountParent;
        entity.AllowsMovements = request.AllowsMovements;
        entity.IsActive        = request.IsActive;

        await db.SaveChangesAsync(ct);

        if (entity.IdAccountParent.HasValue)
            await db.Entry(entity).Reference(a => a.Parent).LoadAsync(ct);

        return ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int idAccount, CancellationToken ct = default)
    {
        var deleted = await db.Account
            .Where(a => a.IdAccount == idAccount)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
