using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankMovements.Dtos;
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

        return await db.BankStatementTransaction
            .AsNoTracking()
            .Where(t => t.IdBankStatementTransaction == entity.IdBankStatementTransaction)
            .Select(MapResponse())
            .FirstAsync(ct);
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
        entity.IdBankMovementType = request.IdBankMovementType;
        entity.IdAccountCounterpart = request.IdAccountCounterpart;
        entity.IdAccountingEntry = request.IdAccountingEntry;

        await db.SaveChangesAsync(ct);

        return await db.BankStatementTransaction
            .AsNoTracking()
            .Where(t => t.IdBankStatementTransaction == idBankStatementTransaction)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
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
            t.IdBankMovementType,
            t.IdBankMovementTypeNavigation != null ? t.IdBankMovementTypeNavigation.NameBankMovementType : null,
            t.IdBankMovementTypeNavigation != null ? t.IdBankMovementTypeNavigation.MovementSign : null,
            t.IdAccountCounterpart,
            t.IdAccountCounterpartNavigation != null ? t.IdAccountCounterpartNavigation.NameAccount : null,
            t.IdAccountingEntry);
    }

    public async Task<BankStatementTransactionResponse?> ClassifyAsync(
        int idBankStatementTransaction,
        ClassifyBankStatementTransactionRequest request,
        CancellationToken ct = default)
    {
        var entity = await db.BankStatementTransaction.FindAsync([idBankStatementTransaction], ct);
        if (entity is null) return null;

        // Validar tipo de movimiento si se especifica
        if (request.IdBankMovementType.HasValue)
        {
            var typeExists = await db.BankMovementType
                .AsNoTracking()
                .AnyAsync(t => t.IdBankMovementType == request.IdBankMovementType.Value, ct);
            if (!typeExists)
                throw new InvalidOperationException($"El tipo de movimiento con ID {request.IdBankMovementType.Value} no existe.");
        }

        // Validar cuenta contrapartida si se especifica
        if (request.IdAccountCounterpart.HasValue)
        {
            var accountExists = await db.Account
                .AsNoTracking()
                .AnyAsync(a => a.IdAccount == request.IdAccountCounterpart.Value, ct);
            if (!accountExists)
                throw new InvalidOperationException($"La cuenta contable con ID {request.IdAccountCounterpart.Value} no existe.");
        }

        entity.IdBankMovementType   = request.IdBankMovementType;
        entity.IdAccountCounterpart = request.IdAccountCounterpart;

        await db.SaveChangesAsync(ct);

        return await db.BankStatementTransaction
            .AsNoTracking()
            .Where(t => t.IdBankStatementTransaction == idBankStatementTransaction)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BankMovementResponse> CreateMovementFromTransactionAsync(
        int idBankStatementTransaction,
        CreateMovementFromTransactionRequest request,
        CancellationToken ct = default)
    {
        var transaction = await db.BankStatementTransaction
            .Include(t => t.IdBankMovementTypeNavigation)
            .FirstOrDefaultAsync(t => t.IdBankStatementTransaction == idBankStatementTransaction, ct)
            ?? throw new InvalidOperationException($"La transacción con ID {idBankStatementTransaction} no existe.");

        if (transaction.IdBankMovementType is null)
            throw new InvalidOperationException(
                "La transacción no tiene un tipo de movimiento asignado. " +
                "Clasifíquela primero mediante PATCH /bank-statement-transactions/{id}/classify.");

        // Validar que el período fiscal existe
        var fiscalPeriodExists = await db.FiscalPeriod
            .AsNoTracking()
            .AnyAsync(fp => fp.IdFiscalPeriod == request.IdFiscalPeriod, ct);
        if (!fiscalPeriodExists)
            throw new InvalidOperationException($"El período fiscal con ID {request.IdFiscalPeriod} no existe.");

        // La cuenta bancaria viene del import → BankAccount
        var bankAccountId = await db.BankStatementImport
            .AsNoTracking()
            .Where(i => i.IdBankStatementImport == transaction.IdBankStatementImport)
            .Select(i => i.IdBankAccount)
            .FirstOrDefaultAsync(ct);

        if (bankAccountId == 0)
            throw new InvalidOperationException("No se pudo determinar la cuenta bancaria de la transacción.");

        // Calcular monto: crédito o débito, el que tenga valor
        var amount = (transaction.CreditAmount ?? 0) > 0
            ? transaction.CreditAmount!.Value
            : (transaction.DebitAmount ?? 0);

        if (amount <= 0)
            throw new InvalidOperationException("El monto de la transacción debe ser mayor a cero.");

        // Generar número de movimiento si no se pasa
        var numberMovement = request.NumberMovement;
        if (string.IsNullOrWhiteSpace(numberMovement))
        {
            var count = await db.BankMovement.CountAsync(ct);
            numberMovement = $"MOV-{transaction.AccountingDate.Year}-{count + 1:D4}";
        }

        var description = string.IsNullOrWhiteSpace(request.DescriptionOverride)
            ? transaction.Description
            : request.DescriptionOverride;

        var movement = new BankMovement
        {
            IdBankAccount      = bankAccountId,
            IdBankMovementType = transaction.IdBankMovementType!.Value,
            IdFiscalPeriod     = request.IdFiscalPeriod,
            NumberMovement     = numberMovement,
            DateMovement       = transaction.AccountingDate,
            DescriptionMovement = description,
            Amount             = amount,
            StatusMovement     = request.StatusMovement,
            ReferenceMovement  = transaction.DocumentNumber,
            ExchangeRateValue  = request.ExchangeRateValue,
            CreatedAt          = DateTime.UtcNow
        };

        db.BankMovement.Add(movement);

        // Marcar transacción como conciliada
        transaction.IsReconciled = true;
        await db.SaveChangesAsync(ct);

        // Cargar navegaciones para el response
        await db.Entry(movement)
            .Reference(m => m.IdBankAccountNavigation)
            .Query()
            .Include(ba => ba.IdAccountNavigation)
            .LoadAsync(ct);

        await db.Entry(movement)
            .Reference(m => m.IdBankMovementTypeNavigation)
            .LoadAsync(ct);

        await db.Entry(movement)
            .Reference(m => m.IdFiscalPeriodNavigation)
            .LoadAsync(ct);

        return new BankMovementResponse(
            movement.IdBankMovement,
            movement.IdBankAccount,
            movement.IdBankAccountNavigation.CodeBankAccount,
            movement.IdBankAccountNavigation.IdAccountNavigation.NameAccount,
            movement.IdBankMovementType,
            movement.IdBankMovementTypeNavigation.CodeBankMovementType,
            movement.IdBankMovementTypeNavigation.NameBankMovementType,
            movement.IdBankMovementTypeNavigation.MovementSign,
            movement.IdFiscalPeriod,
            movement.IdFiscalPeriodNavigation.NamePeriod,
            movement.NumberMovement,
            movement.DateMovement,
            movement.DescriptionMovement,
            movement.Amount,
            movement.StatusMovement,
            movement.ReferenceMovement,
            movement.ExchangeRateValue,
            movement.CreatedAt,
            []);
    }
}
