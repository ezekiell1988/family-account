using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankStatementTransactions.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankStatementTransactions;

public sealed class BankStatementTransactionService(AppDbContext db) : IBankStatementTransactionService
{
    public async Task<IReadOnlyList<BankStatementTransactionResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.BankStatementTransaction
            .AsNoTracking()
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BankStatementTransactionResponse>> GetByImportIdAsync(int idBankStatementImport, CancellationToken ct = default)
    {
        return await db.BankStatementTransaction
            .AsNoTracking()
            .Where(t => t.IdBankStatementImport == idBankStatementImport)
            .OrderBy(t => t.AccountingDate)
            .ThenBy(t => t.TransactionTime)
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<BankStatementTransactionResponse?> GetByIdAsync(int idBankStatementTransaction, CancellationToken ct = default)
    {
        return await db.BankStatementTransaction
            .AsNoTracking()
            .Where(t => t.IdBankStatementTransaction == idBankStatementTransaction)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BankStatementTransactionResponse> CreateAsync(CreateBankStatementTransactionRequest request, CancellationToken ct = default)
    {
        // Validar que la importación existe
        var importExists = await db.BankStatementImport
            .AsNoTracking()
            .AnyAsync(i => i.IdBankStatementImport == request.IdBankStatementImport, ct);

        if (!importExists)
            throw new InvalidOperationException($"La importación con ID {request.IdBankStatementImport} no existe.");

        // Si se especifica un asiento contable, validar que existe
        if (request.IdAccountingEntry.HasValue)
        {
            var accountingEntryExists = await db.AccountingEntry
                .AsNoTracking()
                .AnyAsync(a => a.IdAccountingEntry == request.IdAccountingEntry.Value, ct);

            if (!accountingEntryExists)
                throw new InvalidOperationException($"El asiento contable con ID {request.IdAccountingEntry.Value} no existe.");
        }

        var entity = new BankStatementTransaction
        {
            IdBankStatementImport = request.IdBankStatementImport,
            AccountingDate = request.AccountingDate,
            TransactionDate = request.TransactionDate,
            TransactionTime = request.TransactionTime,
            DocumentNumber = request.DocumentNumber,
            Description = request.Description,
            DebitAmount = request.DebitAmount,
            CreditAmount = request.CreditAmount,
            Balance = request.Balance,
            IsReconciled = false,
            IdAccountingEntry = request.IdAccountingEntry
        };

        db.BankStatementTransaction.Add(entity);
        await db.SaveChangesAsync(ct);

        return new BankStatementTransactionResponse(
            entity.IdBankStatementTransaction,
            entity.IdBankStatementImport,
            entity.AccountingDate,
            entity.TransactionDate,
            entity.TransactionTime,
            entity.DocumentNumber,
            entity.Description,
            entity.DebitAmount,
            entity.CreditAmount,
            entity.Balance,
            entity.IsReconciled,
            entity.IdAccountingEntry);
    }

    public async Task<BankStatementTransactionResponse?> UpdateAsync(int idBankStatementTransaction, UpdateBankStatementTransactionRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankStatementTransaction.FindAsync([idBankStatementTransaction], ct);
        if (entity is null) return null;

        // Si se especifica un asiento contable, validar que existe
        if (request.IdAccountingEntry.HasValue)
        {
            var accountingEntryExists = await db.AccountingEntry
                .AsNoTracking()
                .AnyAsync(a => a.IdAccountingEntry == request.IdAccountingEntry.Value, ct);

            if (!accountingEntryExists)
                throw new InvalidOperationException($"El asiento contable con ID {request.IdAccountingEntry.Value} no existe.");
        }

        entity.AccountingDate = request.AccountingDate;
        entity.TransactionDate = request.TransactionDate;
        entity.TransactionTime = request.TransactionTime;
        entity.DocumentNumber = request.DocumentNumber;
        entity.Description = request.Description;
        entity.DebitAmount = request.DebitAmount;
        entity.CreditAmount = request.CreditAmount;
        entity.Balance = request.Balance;
        entity.IsReconciled = request.IsReconciled;
        entity.IdAccountingEntry = request.IdAccountingEntry;

        await db.SaveChangesAsync(ct);

        return new BankStatementTransactionResponse(
            entity.IdBankStatementTransaction,
            entity.IdBankStatementImport,
            entity.AccountingDate,
            entity.TransactionDate,
            entity.TransactionTime,
            entity.DocumentNumber,
            entity.Description,
            entity.DebitAmount,
            entity.CreditAmount,
            entity.Balance,
            entity.IsReconciled,
            entity.IdAccountingEntry);
    }

    public async Task<bool> DeleteAsync(int idBankStatementTransaction, CancellationToken ct = default)
    {
        var deleted = await db.BankStatementTransaction
            .Where(t => t.IdBankStatementTransaction == idBankStatementTransaction)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    private static System.Linq.Expressions.Expression<Func<BankStatementTransaction, BankStatementTransactionResponse>> MapResponse()
    {
        return t => new BankStatementTransactionResponse(
            t.IdBankStatementTransaction,
            t.IdBankStatementImport,
            t.AccountingDate,
            t.TransactionDate,
            t.TransactionTime,
            t.DocumentNumber,
            t.Description,
            t.DebitAmount,
            t.CreditAmount,
            t.Balance,
            t.IsReconciled,
            t.IdAccountingEntry);
    }
}
