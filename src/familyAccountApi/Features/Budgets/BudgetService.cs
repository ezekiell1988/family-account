using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Budgets.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Budgets;

public sealed class BudgetService(AppDbContext db) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await BuildQuery()
            .OrderByDescending(b => b.IdFiscalPeriodNavigation.YearPeriod)
            .ThenByDescending(b => b.IdFiscalPeriodNavigation.MonthPeriod)
            .ThenBy(b => b.IdAccountNavigation.CodeAccount)
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<BudgetResponse?> GetByIdAsync(int idBudget, CancellationToken ct = default)
    {
        return await BuildQuery()
            .Where(b => b.IdBudget == idBudget)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BudgetResponse> CreateAsync(CreateBudgetRequest request, CancellationToken ct = default)
    {
        await ValidateRequestAsync(request.IdAccount, request.IdFiscalPeriod, ct);

        var entity = new Budget
        {
            IdAccount = request.IdAccount,
            IdFiscalPeriod = request.IdFiscalPeriod,
            AmountBudget = request.AmountBudget,
            NotesBudget = string.IsNullOrWhiteSpace(request.NotesBudget) ? null : request.NotesBudget.Trim(),
            IsActive = request.IsActive
        };

        db.Budget.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.IdBudget, ct))!;
    }

    public async Task<BudgetResponse?> UpdateAsync(int idBudget, UpdateBudgetRequest request, CancellationToken ct = default)
    {
        var entity = await db.Budget.FindAsync([idBudget], ct);
        if (entity is null) return null;

        await ValidateRequestAsync(request.IdAccount, request.IdFiscalPeriod, ct);

        entity.IdAccount = request.IdAccount;
        entity.IdFiscalPeriod = request.IdFiscalPeriod;
        entity.AmountBudget = request.AmountBudget;
        entity.NotesBudget = string.IsNullOrWhiteSpace(request.NotesBudget) ? null : request.NotesBudget.Trim();
        entity.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(idBudget, ct);
    }

    public async Task<bool> DeleteAsync(int idBudget, CancellationToken ct = default)
    {
        var deleted = await db.Budget
            .Where(b => b.IdBudget == idBudget)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    private IQueryable<Budget> BuildQuery()
    {
        return db.Budget
            .AsNoTracking()
            .Include(b => b.IdAccountNavigation)
            .Include(b => b.IdFiscalPeriodNavigation);
    }

    private static System.Linq.Expressions.Expression<Func<Budget, BudgetResponse>> MapResponse()
    {
        return b => new BudgetResponse(
            b.IdBudget,
            b.IdAccount,
            b.IdAccountNavigation.CodeAccount,
            b.IdAccountNavigation.NameAccount,
            b.IdFiscalPeriod,
            b.IdFiscalPeriodNavigation.NamePeriod,
            b.AmountBudget,
            b.NotesBudget,
            b.IsActive);
    }

    private async Task ValidateRequestAsync(int idAccount, int idFiscalPeriod, CancellationToken ct)
    {
        var account = await db.Account
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdAccount == idAccount, ct);

        if (account is null)
            throw new InvalidOperationException("La cuenta contable indicada no existe.");

        if (!account.IsActive)
            throw new InvalidOperationException("No se puede registrar presupuesto sobre una cuenta contable inactiva.");

        var fiscalPeriodExists = await db.FiscalPeriod
            .AsNoTracking()
            .AnyAsync(fp => fp.IdFiscalPeriod == idFiscalPeriod, ct);

        if (!fiscalPeriodExists)
            throw new InvalidOperationException("El período fiscal indicado no existe.");
    }
}