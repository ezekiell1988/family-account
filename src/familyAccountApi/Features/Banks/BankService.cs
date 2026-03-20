using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Banks.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Banks;

public sealed class BankService(AppDbContext db) : IBankService
{
    public async Task<IReadOnlyList<BankResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Bank
            .AsNoTracking()
            .OrderBy(b => b.CodeBank)
            .Select(b => new BankResponse(b.IdBank, b.CodeBank, b.NameBank, b.IsActive))
            .ToListAsync(ct);
    }

    public async Task<BankResponse?> GetByIdAsync(int idBank, CancellationToken ct = default)
    {
        return await db.Bank
            .AsNoTracking()
            .Where(b => b.IdBank == idBank)
            .Select(b => new BankResponse(b.IdBank, b.CodeBank, b.NameBank, b.IsActive))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BankResponse> CreateAsync(CreateBankRequest request, CancellationToken ct = default)
    {
        var entity = new Bank
        {
            CodeBank = request.CodeBank.Trim().ToUpper(),
            NameBank = request.NameBank.Trim(),
            IsActive = request.IsActive
        };

        db.Bank.Add(entity);
        await db.SaveChangesAsync(ct);

        return new BankResponse(entity.IdBank, entity.CodeBank, entity.NameBank, entity.IsActive);
    }

    public async Task<BankResponse?> UpdateAsync(int idBank, UpdateBankRequest request, CancellationToken ct = default)
    {
        var entity = await db.Bank.FindAsync([idBank], ct);
        if (entity is null) return null;

        entity.CodeBank = request.CodeBank.Trim().ToUpper();
        entity.NameBank = request.NameBank.Trim();
        entity.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return new BankResponse(entity.IdBank, entity.CodeBank, entity.NameBank, entity.IsActive);
    }

    public async Task<bool> DeleteAsync(int idBank, CancellationToken ct = default)
    {
        var deleted = await db.Bank
            .Where(b => b.IdBank == idBank)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
