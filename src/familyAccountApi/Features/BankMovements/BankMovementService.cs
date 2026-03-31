using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.BankMovements.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankMovements;

public sealed class BankMovementService(AppDbContext db) : IBankMovementService
{
    public async Task<IReadOnlyList<BankMovementResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await BuildQuery()
            .OrderByDescending(bm => bm.DateMovement)
            .ThenByDescending(bm => bm.IdBankMovement)
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<BankMovementResponse?> GetByIdAsync(int idBankMovement, CancellationToken ct = default)
    {
        return await BuildQuery()
            .Where(bm => bm.IdBankMovement == idBankMovement)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BankMovementResponse> CreateAsync(CreateBankMovementRequest request, CancellationToken ct = default)
    {
        await ValidateRequestAsync(request.IdBankAccount, request.IdBankMovementType, request.IdFiscalPeriod, request.StatusMovement, ct);

        var entity = new BankMovement
        {
            IdBankAccount       = request.IdBankAccount,
            IdBankMovementType  = request.IdBankMovementType,
            IdFiscalPeriod      = request.IdFiscalPeriod,
            NumberMovement      = request.NumberMovement.Trim(),
            DateMovement        = request.DateMovement,
            DescriptionMovement = request.DescriptionMovement.Trim(),
            Amount              = request.Amount,
            StatusMovement      = request.StatusMovement,
            ReferenceMovement   = string.IsNullOrWhiteSpace(request.ReferenceMovement) ? null : request.ReferenceMovement.Trim(),
            ExchangeRateValue   = request.ExchangeRateValue,
            CreatedAt           = DateTime.UtcNow,
            BankMovementDocuments = request.Documents.Select(d => new BankMovementDocument
            {
                IdPurchaseInvoice   = d.IdPurchaseInvoice,
                TypeDocument        = d.TypeDocument.Trim(),
                NumberDocument      = string.IsNullOrWhiteSpace(d.NumberDocument) ? null : d.NumberDocument.Trim(),
                DateDocument        = d.DateDocument,
                AmountDocument      = d.AmountDocument,
                DescriptionDocument = string.IsNullOrWhiteSpace(d.DescriptionDocument) ? null : d.DescriptionDocument.Trim()
            }).ToList()
        };

        db.BankMovement.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.IdBankMovement, ct))!;
    }

    public async Task<BankMovementResponse?> UpdateAsync(int idBankMovement, UpdateBankMovementRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankMovement
            .Include(bm => bm.BankMovementDocuments)
            .FirstOrDefaultAsync(bm => bm.IdBankMovement == idBankMovement, ct);

        if (entity is null) return null;

        if (entity.StatusMovement == "Anulado")
            throw new InvalidOperationException("No se puede modificar un movimiento anulado.");

        await ValidateRequestAsync(request.IdBankAccount, request.IdBankMovementType, request.IdFiscalPeriod, request.StatusMovement, ct);

        entity.IdBankAccount       = request.IdBankAccount;
        entity.IdBankMovementType  = request.IdBankMovementType;
        entity.IdFiscalPeriod      = request.IdFiscalPeriod;
        entity.NumberMovement      = request.NumberMovement.Trim();
        entity.DateMovement        = request.DateMovement;
        entity.DescriptionMovement = request.DescriptionMovement.Trim();
        entity.Amount              = request.Amount;
        entity.StatusMovement      = request.StatusMovement;
        entity.ReferenceMovement   = string.IsNullOrWhiteSpace(request.ReferenceMovement) ? null : request.ReferenceMovement.Trim();
        entity.ExchangeRateValue   = request.ExchangeRateValue;

        db.BankMovementDocument.RemoveRange(entity.BankMovementDocuments);
        entity.BankMovementDocuments.Clear();

        foreach (var d in request.Documents)
        {
            entity.BankMovementDocuments.Add(new BankMovementDocument
            {
                IdPurchaseInvoice   = d.IdPurchaseInvoice,
                TypeDocument        = d.TypeDocument.Trim(),
                NumberDocument      = string.IsNullOrWhiteSpace(d.NumberDocument) ? null : d.NumberDocument.Trim(),
                DateDocument        = d.DateDocument,
                AmountDocument      = d.AmountDocument,
                DescriptionDocument = string.IsNullOrWhiteSpace(d.DescriptionDocument) ? null : d.DescriptionDocument.Trim()
            });
        }

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(entity.IdBankMovement, ct);
    }

    public async Task<bool> DeleteAsync(int idBankMovement, CancellationToken ct = default)
    {
        var movement = await db.BankMovement
            .AsNoTracking()
            .Where(bm => bm.IdBankMovement == idBankMovement)
            .Select(bm => new { bm.StatusMovement })
            .FirstOrDefaultAsync(ct);

        if (movement is null) return false;

        if (movement.StatusMovement != "Borrador")
            throw new InvalidOperationException("Solo se pueden eliminar movimientos en estado 'Borrador'.");

        var deleted = await db.BankMovement
            .Where(bm => bm.IdBankMovement == idBankMovement)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    public async Task<BankMovementResponse?> ConfirmAsync(int idBankMovement, CancellationToken ct = default)
    {
        var entity = await db.BankMovement
            .Include(bm => bm.IdBankAccountNavigation)
                .ThenInclude(ba => ba.IdCurrencyNavigation)
            .Include(bm => bm.IdBankMovementTypeNavigation)
            .Include(bm => bm.BankMovementDocuments)
            .FirstOrDefaultAsync(bm => bm.IdBankMovement == idBankMovement, ct);

        if (entity is null) return null;

        if (entity.StatusMovement != "Borrador")
            throw new InvalidOperationException($"Solo se puede confirmar un movimiento en estado 'Borrador'. Estado actual: '{entity.StatusMovement}'.");

        // ── Crear asiento contable automático ────────────────────────────
        var bankAccount     = entity.IdBankAccountNavigation;
        var movementType    = entity.IdBankMovementTypeNavigation;
        var isCargo         = movementType.MovementSign == "Cargo";

        var accountingEntry = new AccountingEntry
        {
            IdFiscalPeriod    = entity.IdFiscalPeriod,
            IdCurrency        = bankAccount.IdCurrency,
            NumberEntry       = $"ASI-{entity.NumberMovement}",
            DateEntry         = entity.DateMovement,
            DescriptionEntry  = entity.DescriptionMovement,
            StatusEntry       = "Publicado",
            ReferenceEntry    = entity.NumberMovement,
            ExchangeRateValue = entity.ExchangeRateValue,
            OriginModule      = "BankMovement",
            IdOriginRecord    = entity.IdBankMovement,
            CreatedAt         = DateTime.UtcNow,
            AccountingEntryLines =
            [
                new AccountingEntryLine
                {
                    IdAccount       = isCargo ? bankAccount.IdAccount : movementType.IdAccountCounterpart,
                    DebitAmount     = entity.Amount,
                    CreditAmount    = 0,
                    DescriptionLine = entity.DescriptionMovement
                },
                new AccountingEntryLine
                {
                    IdAccount       = isCargo ? movementType.IdAccountCounterpart : bankAccount.IdAccount,
                    DebitAmount     = 0,
                    CreditAmount    = entity.Amount,
                    DescriptionLine = entity.DescriptionMovement
                },
            ]
        };

        db.AccountingEntry.Add(accountingEntry);
        await db.SaveChangesAsync(ct);

        // Vincular el asiento al movimiento bancario
        entity.IdAccountingEntry = accountingEntry.IdAccountingEntry;
        entity.StatusMovement    = "Confirmado";
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(idBankMovement, ct);
    }

    public async Task<BankMovementResponse?> CancelAsync(int idBankMovement, CancellationToken ct = default)
    {
        var entity = await db.BankMovement.FindAsync([idBankMovement], ct);
        if (entity is null) return null;

        if (entity.StatusMovement == "Anulado")
            throw new InvalidOperationException("El movimiento ya está anulado.");

        entity.StatusMovement = "Anulado";

        // Anular asiento contable vinculado al movimiento bancario
        if (entity.IdAccountingEntry.HasValue)
            await db.AccountingEntry
                .Where(ae => ae.IdAccountingEntry == entity.IdAccountingEntry.Value && ae.StatusEntry != "Anulado")
                .ExecuteUpdateAsync(s => s.SetProperty(ae => ae.StatusEntry, "Anulado"), ct);

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(idBankMovement, ct);
    }

    private IQueryable<BankMovement> BuildQuery()
    {
        return db.BankMovement
            .AsNoTracking()
            .Include(bm => bm.IdBankAccountNavigation)
            .Include(bm => bm.IdBankMovementTypeNavigation)
            .Include(bm => bm.IdFiscalPeriodNavigation)
            .Include(bm => bm.BankMovementDocuments)
                .ThenInclude(d => d.IdPurchaseInvoiceNavigation);
    }

    private static System.Linq.Expressions.Expression<Func<BankMovement, BankMovementResponse>> MapResponse()
    {
        return bm => new BankMovementResponse(
            bm.IdBankMovement,
            bm.IdBankAccount,
            bm.IdBankAccountNavigation.CodeBankAccount,
            bm.IdBankAccountNavigation.AccountNumber,
            bm.IdBankMovementType,
            bm.IdBankMovementTypeNavigation.CodeBankMovementType,
            bm.IdBankMovementTypeNavigation.NameBankMovementType,
            bm.IdBankMovementTypeNavigation.MovementSign,
            bm.IdFiscalPeriod,
            bm.IdFiscalPeriodNavigation.NamePeriod,
            bm.NumberMovement,
            bm.DateMovement,
            bm.DescriptionMovement,
            bm.Amount,
            bm.StatusMovement,
            bm.ReferenceMovement,
            bm.ExchangeRateValue,
            bm.CreatedAt,
            bm.IdAccountingEntry,
            bm.BankMovementDocuments
                .Select(d => new BankMovementDocumentResponse(
                    d.IdBankMovementDocument,
                    d.IdPurchaseInvoice,
                    d.IdPurchaseInvoiceNavigation != null ? d.IdPurchaseInvoiceNavigation.NumberInvoice : null,
                    d.TypeDocument,
                    d.NumberDocument,
                    d.DateDocument,
                    d.AmountDocument,
                    d.DescriptionDocument))
                .ToList());
    }

    private async Task ValidateRequestAsync(int idBankAccount, int idBankMovementType, int idFiscalPeriod, string statusMovement, CancellationToken ct)
    {
        var bankAccountExists = await db.BankAccount
            .AsNoTracking()
            .AnyAsync(ba => ba.IdBankAccount == idBankAccount && ba.IsActive, ct);

        if (!bankAccountExists)
            throw new InvalidOperationException($"La cuenta bancaria con ID {idBankAccount} no existe o no está activa.");

        var movementTypeExists = await db.BankMovementType
            .AsNoTracking()
            .AnyAsync(bmt => bmt.IdBankMovementType == idBankMovementType && bmt.IsActive, ct);

        if (!movementTypeExists)
            throw new InvalidOperationException($"El tipo de movimiento con ID {idBankMovementType} no existe o no está activo.");

        var fiscalPeriodExists = await db.FiscalPeriod
            .AsNoTracking()
            .AnyAsync(fp => fp.IdFiscalPeriod == idFiscalPeriod, ct);

        if (!fiscalPeriodExists)
            throw new InvalidOperationException($"El período fiscal con ID {idFiscalPeriod} no existe.");

        if (statusMovement is not ("Borrador" or "Confirmado" or "Anulado"))
            throw new InvalidOperationException($"Estado de movimiento inválido: '{statusMovement}'. Use 'Borrador', 'Confirmado' o 'Anulado'.");
    }
}
