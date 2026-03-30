using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.AccountingEntries.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.AccountingEntries;

public sealed class AccountingEntryService(AppDbContext db) : IAccountingEntryService
{
    public async Task<IReadOnlyList<AccountingEntryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await BuildQuery()
            .OrderByDescending(ae => ae.DateEntry)
            .ThenByDescending(ae => ae.IdAccountingEntry)
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<AccountingEntryResponse?> GetByIdAsync(int idAccountingEntry, CancellationToken ct = default)
    {
        return await BuildQuery()
            .Where(ae => ae.IdAccountingEntry == idAccountingEntry)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<AccountingEntryResponse> CreateAsync(CreateAccountingEntryRequest request, CancellationToken ct = default)
    {
        await ValidateRequestAsync(request.IdFiscalPeriod, request.IdCurrency, request.ExchangeRateValue, request.StatusEntry, request.Lines, ct);

        var targetStatus = request.StatusEntry;
        var entity = new AccountingEntry
        {
            IdFiscalPeriod   = request.IdFiscalPeriod,
            IdCurrency       = request.IdCurrency,
            NumberEntry      = request.NumberEntry.Trim(),
            DateEntry        = request.DateEntry,
            DescriptionEntry = request.DescriptionEntry.Trim(),
            StatusEntry      = targetStatus == "Publicado" ? "Borrador" : targetStatus,
            ReferenceEntry   = string.IsNullOrWhiteSpace(request.ReferenceEntry) ? null : request.ReferenceEntry.Trim(),
            ExchangeRateValue = request.ExchangeRateValue,
            AccountingEntryLines = request.Lines.Select(line => new AccountingEntryLine
            {
                IdAccount       = line.IdAccount,
                DebitAmount     = line.DebitAmount,
                CreditAmount    = line.CreditAmount,
                DescriptionLine = string.IsNullOrWhiteSpace(line.DescriptionLine) ? null : line.DescriptionLine.Trim()
            }).ToList()
        };

        db.AccountingEntry.Add(entity);
        await db.SaveChangesAsync(ct);

        if (targetStatus == "Publicado")
        {
            entity.StatusEntry = "Publicado";
            await db.SaveChangesAsync(ct);
        }

        return (await GetByIdAsync(entity.IdAccountingEntry, ct))!;
    }

    public async Task<AccountingEntryResponse?> UpdateAsync(int idAccountingEntry, UpdateAccountingEntryRequest request, CancellationToken ct = default)
    {
        var entity = await db.AccountingEntry
            .Include(ae => ae.AccountingEntryLines)
            .FirstOrDefaultAsync(ae => ae.IdAccountingEntry == idAccountingEntry, ct);

        if (entity is null) return null;

        if (entity.StatusEntry == "Anulado")
            throw new InvalidOperationException("No se puede modificar un asiento anulado.");

        await ValidateRequestAsync(request.IdFiscalPeriod, request.IdCurrency, request.ExchangeRateValue, request.StatusEntry, request.Lines, ct);

        var targetStatus = request.StatusEntry;

        entity.IdFiscalPeriod   = request.IdFiscalPeriod;
        entity.IdCurrency       = request.IdCurrency;
        entity.NumberEntry      = request.NumberEntry.Trim();
        entity.DateEntry        = request.DateEntry;
        entity.DescriptionEntry = request.DescriptionEntry.Trim();
        entity.ReferenceEntry   = string.IsNullOrWhiteSpace(request.ReferenceEntry) ? null : request.ReferenceEntry.Trim();
        entity.ExchangeRateValue = request.ExchangeRateValue;
        entity.StatusEntry      = targetStatus == "Publicado" ? "Borrador" : targetStatus;

        db.AccountingEntryLine.RemoveRange(entity.AccountingEntryLines);
        entity.AccountingEntryLines.Clear();

        foreach (var line in request.Lines)
        {
            entity.AccountingEntryLines.Add(new AccountingEntryLine
            {
                IdAccount       = line.IdAccount,
                DebitAmount     = line.DebitAmount,
                CreditAmount    = line.CreditAmount,
                DescriptionLine = string.IsNullOrWhiteSpace(line.DescriptionLine) ? null : line.DescriptionLine.Trim()
            });
        }

        await db.SaveChangesAsync(ct);

        if (targetStatus == "Publicado")
        {
            entity.StatusEntry = "Publicado";
            await db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(entity.IdAccountingEntry, ct);
    }

    public async Task<bool> DeleteAsync(int idAccountingEntry, CancellationToken ct = default)
    {
        var deleted = await db.AccountingEntry
            .Where(ae => ae.IdAccountingEntry == idAccountingEntry)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    private IQueryable<AccountingEntry> BuildQuery()
    {
        return db.AccountingEntry
            .AsNoTracking()
            .OrderBy(ae => ae.IdAccountingEntry)
            .Include(ae => ae.IdFiscalPeriodNavigation)
            .Include(ae => ae.IdCurrencyNavigation)
            .Include(ae => ae.AccountingEntryLines)
                .ThenInclude(line => line.IdAccountNavigation);
    }

    private System.Linq.Expressions.Expression<Func<AccountingEntry, AccountingEntryResponse>> MapResponse()
    {
        return ae => new AccountingEntryResponse(
            ae.IdAccountingEntry,
            ae.IdFiscalPeriod,
            ae.IdFiscalPeriodNavigation.NamePeriod,
            ae.IdCurrency,
            ae.IdCurrencyNavigation.CodeCurrency,
            ae.IdCurrencyNavigation.NameCurrency,
            ae.NumberEntry,
            ae.DateEntry,
            ae.DescriptionEntry,
            ae.StatusEntry,
            ae.ReferenceEntry,
            ae.ExchangeRateValue,
            ae.CreatedAt,
            ae.AccountingEntryLines
                .OrderBy(line => line.IdAccountingEntryLine)
                .Select(line => new AccountingEntryLineResponse(
                    line.IdAccountingEntryLine,
                    line.IdAccount,
                    line.IdAccountNavigation.CodeAccount,
                    line.IdAccountNavigation.NameAccount,
                    line.DebitAmount,
                    line.CreditAmount,
                    line.DescriptionLine))
                .ToList(),
            db.BankMovement.Any(bm => bm.IdAccountingEntry == ae.IdAccountingEntry) ||
            db.PurchaseInvoiceEntry.Any(pe => pe.IdAccountingEntry == ae.IdAccountingEntry));
    }

    private async Task ValidateRequestAsync(
        int idFiscalPeriod,
        int idCurrency,
        decimal exchangeRateValue,
        string statusEntry,
        IReadOnlyList<AccountingEntryLineRequest> lines,
        CancellationToken ct)
    {
        if (statusEntry is not ("Borrador" or "Publicado" or "Anulado"))
            throw new InvalidOperationException("El estado del asiento debe ser Borrador, Publicado o Anulado.");

        if (lines.Count < 2)
            throw new InvalidOperationException("El asiento debe tener al menos dos líneas.");

        var fiscalPeriod = await db.FiscalPeriod
            .AsNoTracking()
            .FirstOrDefaultAsync(fp => fp.IdFiscalPeriod == idFiscalPeriod, ct);

        if (fiscalPeriod is null)
            throw new InvalidOperationException("El período fiscal indicado no existe.");

        if (fiscalPeriod.StatusPeriod != "Abierto")
            throw new InvalidOperationException("Solo se pueden registrar asientos en períodos fiscales abiertos.");

        var currencyExists = await db.Currency
            .AsNoTracking()
            .AnyAsync(c => c.IdCurrency == idCurrency, ct);

        if (!currencyExists)
            throw new InvalidOperationException("La moneda indicada no existe.");

        if (exchangeRateValue <= 0)
            throw new InvalidOperationException("El tipo de cambio del asiento debe ser mayor que cero.");

        var totalDebit = 0m;
        var totalCredit = 0m;

        foreach (var line in lines)
        {
            var hasDebit = line.DebitAmount > 0;
            var hasCredit = line.CreditAmount > 0;

            if (hasDebit == hasCredit)
                throw new InvalidOperationException("Cada línea debe tener monto solo al débito o solo al crédito.");

            totalDebit += line.DebitAmount;
            totalCredit += line.CreditAmount;
        }

        if (totalDebit <= 0 || totalCredit <= 0)
            throw new InvalidOperationException("El asiento debe tener montos válidos al débito y al crédito.");

        if (totalDebit != totalCredit)
            throw new InvalidOperationException("El asiento contable está desbalanceado: la suma del débito debe ser igual a la suma del crédito.");

        var accountIds = lines.Select(line => line.IdAccount).Distinct().ToList();
        var accounts = await db.Account
            .AsNoTracking()
            .Where(account => accountIds.Contains(account.IdAccount))
            .ToListAsync(ct);

        if (accounts.Count != accountIds.Count)
            throw new InvalidOperationException("Una o más cuentas contables indicadas no existen.");

        if (accounts.Any(account => !account.IsActive))
            throw new InvalidOperationException("No se puede usar una cuenta contable inactiva en un asiento.");

        if (accounts.Any(account => !account.AllowsMovements))
            throw new InvalidOperationException("Todas las cuentas del asiento deben permitir movimientos contables directos.");
    }
}
