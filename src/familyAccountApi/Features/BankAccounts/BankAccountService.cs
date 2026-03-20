using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankAccounts.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankAccounts;

public sealed class BankAccountService(AppDbContext db) : IBankAccountService
{
    public async Task<IReadOnlyList<BankAccountResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await BuildQuery()
            .OrderBy(ba => ba.CodeBankAccount)
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<BankAccountResponse?> GetByIdAsync(int idBankAccount, CancellationToken ct = default)
    {
        return await BuildQuery()
            .Where(ba => ba.IdBankAccount == idBankAccount)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BankAccountResponse> CreateAsync(CreateBankAccountRequest request, CancellationToken ct = default)
    {
        await ValidateRequestAsync(request.IdAccount, request.IdCurrency, ct);

        var entity = new BankAccount
        {
            IdAccount = request.IdAccount,
            IdCurrency = request.IdCurrency,
            CodeBankAccount = request.CodeBankAccount.Trim(),
            BankName = request.BankName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            AccountHolder = request.AccountHolder.Trim(),
            IsActive = request.IsActive
        };

        db.BankAccount.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.IdBankAccount, ct))!;
    }

    public async Task<BankAccountResponse?> UpdateAsync(int idBankAccount, UpdateBankAccountRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankAccount.FindAsync([idBankAccount], ct);
        if (entity is null) return null;

        await ValidateRequestAsync(request.IdAccount, request.IdCurrency, ct);

        entity.IdAccount = request.IdAccount;
        entity.IdCurrency = request.IdCurrency;
        entity.CodeBankAccount = request.CodeBankAccount.Trim();
        entity.BankName = request.BankName.Trim();
        entity.AccountNumber = request.AccountNumber.Trim();
        entity.AccountHolder = request.AccountHolder.Trim();
        entity.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(idBankAccount, ct);
    }

    public async Task<bool> DeleteAsync(int idBankAccount, CancellationToken ct = default)
    {
        var deleted = await db.BankAccount
            .Where(ba => ba.IdBankAccount == idBankAccount)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    private IQueryable<BankAccount> BuildQuery()
    {
        return db.BankAccount
            .AsNoTracking()
            .Include(ba => ba.IdAccountNavigation)
            .Include(ba => ba.IdCurrencyNavigation);
    }

    private static System.Linq.Expressions.Expression<Func<BankAccount, BankAccountResponse>> MapResponse()
    {
        return ba => new BankAccountResponse(
            ba.IdBankAccount,
            ba.IdAccount,
            ba.IdAccountNavigation.CodeAccount,
            ba.IdAccountNavigation.NameAccount,
            ba.IdCurrency,
            ba.IdCurrencyNavigation.CodeCurrency,
            ba.IdCurrencyNavigation.NameCurrency,
            ba.CodeBankAccount,
            ba.BankName,
            ba.AccountNumber,
            ba.AccountHolder,
            ba.IsActive);
    }

    private async Task ValidateRequestAsync(int idAccount, int idCurrency, CancellationToken ct)
    {
        var account = await db.Account
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdAccount == idAccount, ct);

        if (account is null)
            throw new InvalidOperationException("La cuenta contable indicada no existe.");

        if (!account.IsActive)
            throw new InvalidOperationException("No se puede vincular una cuenta bancaria a una cuenta contable inactiva.");

        if (!account.AllowsMovements)
            throw new InvalidOperationException("La cuenta contable vinculada a una cuenta bancaria debe permitir movimientos.");

        var currencyExists = await db.Currency
            .AsNoTracking()
            .AnyAsync(c => c.IdCurrency == idCurrency, ct);

        if (!currencyExists)
            throw new InvalidOperationException("La moneda indicada no existe.");
    }
}