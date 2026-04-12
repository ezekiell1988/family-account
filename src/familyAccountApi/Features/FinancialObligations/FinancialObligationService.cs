using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.FinancialObligations.Dtos;
using FamilyAccountApi.Features.FinancialObligations.Parsers;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.FinancialObligations;

public sealed class FinancialObligationService(AppDbContext db) : IFinancialObligationService
{
    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FinancialObligationResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await BuildBaseQuery()
            .Select(MapResponse())
            .ToListAsync(ct);
    }

    public async Task<FinancialObligationResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await BuildBaseQuery()
            .Where(o => o.IdFinancialObligation == id)
            .Select(MapResponse())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FinancialObligationSummaryResponse?> GetSummaryAsync(int id, CancellationToken ct = default)
    {
        var obligation = await db.FinancialObligation
            .AsNoTracking()
            .Include(o => o.IdCurrencyNavigation)
            .Include(o => o.FinancialObligationInstallments)
                .ThenInclude(i => i.FinancialObligationPayment)
            .FirstOrDefaultAsync(o => o.IdFinancialObligation == id, ct);

        if (obligation is null) return null;

        var today     = DateOnly.FromDateTime(DateTime.Today);
        var horizon   = today.AddYears(1);
        var paid      = obligation.FinancialObligationInstallments.Where(i => i.StatusInstallment == "Pagada").ToList();
        var pending   = obligation.FinancialObligationInstallments.Where(i => i.StatusInstallment is "Pendiente" or "Vigente").ToList();
        var current   = obligation.FinancialObligationInstallments.FirstOrDefault(i => i.StatusInstallment == "Vigente")
                     ?? obligation.FinancialObligationInstallments.Where(i => i.StatusInstallment == "Pendiente").OrderBy(i => i.DueDate).FirstOrDefault();
        var next      = obligation.FinancialObligationInstallments
                         .Where(i => i.StatusInstallment is "Pendiente" or "Vigente" && i != current)
                         .OrderBy(i => i.DueDate).FirstOrDefault();

        var lastPaid  = paid.OrderByDescending(i => i.NumberInstallment).FirstOrDefault();
        var balance   = lastPaid?.BalanceAfter ?? obligation.OriginalAmount;

        var portionCurrent = obligation.FinancialObligationInstallments
            .Where(i => i.StatusInstallment is "Pendiente" or "Vigente" && i.DueDate >= today && i.DueDate <= horizon)
            .Sum(i => i.AmountCapital);

        return new FinancialObligationSummaryResponse(
            IdFinancialObligation:    id,
            NameObligation:           obligation.NameObligation,
            CodeCurrency:             obligation.IdCurrencyNavigation.CodeCurrency,
            OriginalAmount:           obligation.OriginalAmount,
            CurrentBalance:           balance,
            TotalCapitalPaid:         paid.Sum(i => i.FinancialObligationPayment?.AmountCapitalPaid ?? 0),
            TotalInterestPaid:        paid.Sum(i => i.FinancialObligationPayment?.AmountInterestPaid ?? 0),
            TotalLatePaid:            paid.Sum(i => i.FinancialObligationPayment?.AmountLatePaid ?? 0),
            PortionCurrentYear:       portionCurrent,
            CurrentInstallmentNumber: current?.NumberInstallment,
            CurrentInstallmentDue:    current?.DueDate,
            CurrentInstallmentTotal:  current?.AmountTotal,
            NextInstallmentNumber:    next?.NumberInstallment,
            NextInstallmentDue:       next?.DueDate,
            NextInstallmentTotal:     next?.AmountTotal,
            InstallmentsPaid:         paid.Count,
            InstallmentsPending:      pending.Count,
            StatusObligation:         obligation.StatusObligation);
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public async Task<FinancialObligationResponse> CreateAsync(CreateFinancialObligationRequest req, CancellationToken ct = default)
    {
        await ValidateAccountsAsync(req.IdAccountLongTerm, req.IdAccountShortTerm, req.IdAccountInterest,
            req.IdAccountLateFee, req.IdAccountOther, ct);

        var entity = new FinancialObligation
        {
            NameObligation       = req.NameObligation.Trim(),
            IdCurrency           = req.IdCurrency,
            OriginalAmount       = req.OriginalAmount,
            InterestRate         = req.InterestRate,
            StartDate            = req.StartDate,
            TermMonths           = req.TermMonths,
            IdBankAccountPayment = req.IdBankAccountPayment,
            IdAccountLongTerm    = req.IdAccountLongTerm,
            IdAccountShortTerm   = req.IdAccountShortTerm,
            IdAccountInterest    = req.IdAccountInterest,
            IdAccountLateFee     = req.IdAccountLateFee,
            IdAccountOther       = req.IdAccountOther,
            MatchKeyword         = req.MatchKeyword.Trim().ToUpper(),
            StatusObligation     = "Activo",
            Notes                = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
        };

        db.FinancialObligation.Add(entity);
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(entity.IdFinancialObligation, ct))!;
    }

    public async Task<FinancialObligationResponse?> UpdateAsync(int id, UpdateFinancialObligationRequest req, CancellationToken ct = default)
    {
        var entity = await db.FinancialObligation.FirstOrDefaultAsync(o => o.IdFinancialObligation == id, ct);
        if (entity is null) return null;

        await ValidateAccountsAsync(req.IdAccountLongTerm, req.IdAccountShortTerm, req.IdAccountInterest,
            req.IdAccountLateFee, req.IdAccountOther, ct);

        entity.NameObligation       = req.NameObligation.Trim();
        entity.IdBankAccountPayment = req.IdBankAccountPayment;
        entity.IdAccountLongTerm    = req.IdAccountLongTerm;
        entity.IdAccountShortTerm   = req.IdAccountShortTerm;
        entity.IdAccountInterest    = req.IdAccountInterest;
        entity.IdAccountLateFee     = req.IdAccountLateFee;
        entity.IdAccountOther       = req.IdAccountOther;
        entity.MatchKeyword         = req.MatchKeyword.Trim().ToUpper();
        entity.StatusObligation     = req.StatusObligation;
        entity.Notes                = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();

        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(id, ct))!;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.FinancialObligation
            .Include(o => o.FinancialObligationInstallments)
                .ThenInclude(i => i.FinancialObligationPayment)
            .FirstOrDefaultAsync(o => o.IdFinancialObligation == id, ct);

        if (entity is null) return false;

        var hasPayments = entity.FinancialObligationInstallments
            .Any(i => i.FinancialObligationPayment is not null);

        if (hasPayments)
            throw new InvalidOperationException("No se puede eliminar un préstamo con pagos registrados.");

        db.FinancialObligation.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Sync Excel ────────────────────────────────────────────────────────────

    public async Task<SyncExcelResult> SyncExcelAsync(int id, Stream fileStream, CancellationToken ct = default)
    {
        var obligation = await db.FinancialObligation
            .Include(o => o.FinancialObligationInstallments)
                .ThenInclude(i => i.FinancialObligationPayment)
            .FirstOrDefaultAsync(o => o.IdFinancialObligation == id, ct)
            ?? throw new InvalidOperationException($"Obligación {id} no encontrada.");

        var parser = new FinancialObligationExcelParser();
        var rows   = parser.Parse(fileStream);

        var warnings          = new List<string>();
        var installmentsUpserted = 0;
        var paymentsCreated   = 0;
        var paymentsSkipped   = 0;
        var now               = DateTime.UtcNow;

        // ── Upsert de cuotas ─────────────────────────────────────────────────
        foreach (var row in rows)
        {
            var existing = obligation.FinancialObligationInstallments
                .FirstOrDefault(i => i.NumberInstallment == row.NumberInstallment);

            if (existing is null)
            {
                existing = new FinancialObligationInstallment
                {
                    IdFinancialObligation = id,
                    NumberInstallment     = row.NumberInstallment
                };
                // EF Core relationship fixup agrega automáticamente la entidad a
                // obligation.FinancialObligationInstallments — no agregar manualmente
                // para evitar duplicados en la colección en memoria.
                db.FinancialObligationInstallment.Add(existing);
            }

            existing.DueDate           = row.DueDate;
            existing.BalanceAfter      = row.BalanceAfter;
            existing.AmountCapital     = row.AmountCapital;
            existing.AmountInterest    = row.AmountInterest;
            existing.AmountLateFee     = row.AmountLateFee;
            existing.AmountOther       = row.AmountOther;
            existing.AmountTotal       = row.AmountTotal;
            existing.StatusInstallment = row.StatusInstallment;
            existing.SyncedAt          = now;
            installmentsUpserted++;

            // ── Auto-pago si el Excel dice Pagada y no tiene pago ────────────
            if (row.StatusInstallment == "Pagada" && existing.FinancialObligationPayment is null)
            {
                await db.SaveChangesAsync(ct); // asegurar Id antes de buscar match

                var (bankMovementId, paymentDate, matchWarning) =
                    await FindBankMovementAsync(obligation, existing, ct);

                if (matchWarning is not null) warnings.Add(matchWarning);

                if (bankMovementId is null && paymentDate is null)
                {
                    paymentsSkipped++;
                    continue;
                }

                var effectiveDate = paymentDate ?? row.DueDate;
                var period        = await FindOrCreateFiscalPeriodIdAsync(effectiveDate, ct);
                var entry         = BuildPaymentEntry(obligation, existing, effectiveDate, period);
                db.AccountingEntry.Add(entry);
                await db.SaveChangesAsync(ct);

                var payment = new FinancialObligationPayment
                {
                    IdFinancialObligationInstallment = existing.IdFinancialObligationInstallment,
                    IdBankMovement                   = bankMovementId,
                    DatePayment                      = effectiveDate,
                    AmountPaid                       = row.AmountTotal,
                    AmountCapitalPaid                = row.AmountCapital,
                    AmountInterestPaid               = row.AmountInterest,
                    AmountLatePaid                   = row.AmountLateFee,
                    AmountOtherPaid                  = row.AmountOther,
                    IdAccountingEntry                = entry.IdAccountingEntry,
                    IsAutoProcessed                  = true
                };
                db.FinancialObligationPayment.Add(payment);
                existing.FinancialObligationPayment = payment;
                paymentsCreated++;
            }
        }
        await db.SaveChangesAsync(ct);

        // ── Reclasificación automática ────────────────────────────────────────
        var today             = DateOnly.FromDateTime(DateTime.Today);
        var previousPortion   = await GetLastReclassifiedPortionAsync(id, ct);
        var newPortion        = CalculateShortTermPortion(obligation, today);
        int? reclassEntryId  = null;

        if (Math.Abs(newPortion - previousPortion) >= 0.01m && newPortion > 0)
        {
            var period = await FindOrCreateFiscalPeriodIdAsync(today, ct);
            var entry  = BuildReclassificationEntry(obligation, newPortion, today, period);
            db.AccountingEntry.Add(entry);
            await db.SaveChangesAsync(ct);
            reclassEntryId = entry.IdAccountingEntry;
        }

        return new SyncExcelResult(
            InstallmentsUpserted:       installmentsUpserted,
            PaymentsCreated:            paymentsCreated,
            PaymentsSkipped:            paymentsSkipped,
            ReclassificationEntryId:    reclassEntryId,
            PreviousShortTermPortion:   previousPortion,
            NewShortTermPortion:        newPortion,
            Warnings:                   warnings);
    }

    // ── Pago manual ───────────────────────────────────────────────────────────

    public async Task<FinancialObligationPaymentResponse> RegisterPaymentAsync(
        int installmentId, RegisterPaymentRequest req, CancellationToken ct = default)
    {
        var installment = await db.FinancialObligationInstallment
            .Include(i => i.IdFinancialObligationNavigation)
            .Include(i => i.FinancialObligationPayment)
            .FirstOrDefaultAsync(i => i.IdFinancialObligationInstallment == installmentId, ct)
            ?? throw new InvalidOperationException($"Cuota {installmentId} no encontrada.");

        if (installment.FinancialObligationPayment is not null)
            throw new InvalidOperationException("Esta cuota ya tiene un pago registrado.");

        var obligation = installment.IdFinancialObligationNavigation;
        var period     = await FindOrCreateFiscalPeriodIdAsync(req.DatePayment, ct);
        var entry      = BuildPaymentEntryFromRequest(obligation, installment, req, period);
        db.AccountingEntry.Add(entry);
        await db.SaveChangesAsync(ct);

        var payment = new FinancialObligationPayment
        {
            IdFinancialObligationInstallment = installmentId,
            IdBankMovement                   = req.IdBankMovement,
            DatePayment                      = req.DatePayment,
            AmountPaid                       = req.AmountPaid,
            AmountCapitalPaid                = req.AmountCapitalPaid,
            AmountInterestPaid               = req.AmountInterestPaid,
            AmountLatePaid                   = req.AmountLatePaid,
            AmountOtherPaid                  = req.AmountOtherPaid,
            IdAccountingEntry                = entry.IdAccountingEntry,
            IsAutoProcessed                  = false,
            Notes                            = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
        };
        db.FinancialObligationPayment.Add(payment);

        installment.StatusInstallment = "Pagada";
        await db.SaveChangesAsync(ct);

        return MapPayment(payment);
    }

    // ── Reclasificación manual ────────────────────────────────────────────────

    public async Task<int?> ReclassifyAsync(int id, DateOnly asOfDate, CancellationToken ct = default)
    {
        var obligation = await db.FinancialObligation
            .Include(o => o.FinancialObligationInstallments)
            .FirstOrDefaultAsync(o => o.IdFinancialObligation == id, ct)
            ?? throw new InvalidOperationException($"Obligación {id} no encontrada.");

        var portion = CalculateShortTermPortion(obligation, asOfDate);
        if (portion <= 0) return null;

        var period = await FindOrCreateFiscalPeriodIdAsync(asOfDate, ct);
        var entry  = BuildReclassificationEntry(obligation, portion, asOfDate, period);
        db.AccountingEntry.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.IdAccountingEntry;
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private IQueryable<FinancialObligation> BuildBaseQuery() =>
        db.FinancialObligation
            .AsNoTracking()
            .Include(o => o.IdCurrencyNavigation)
            .Include(o => o.IdBankAccountPaymentNavigation)
            .Include(o => o.IdAccountLongTermNavigation)
            .Include(o => o.IdAccountShortTermNavigation)
            .Include(o => o.IdAccountInterestNavigation)
            .Include(o => o.IdAccountLateFeeNavigation)
            .Include(o => o.IdAccountOtherNavigation)
            .Include(o => o.FinancialObligationInstallments.OrderBy(i => i.NumberInstallment))
                .ThenInclude(i => i.FinancialObligationPayment)
            .OrderBy(o => o.NameObligation);

    private static System.Linq.Expressions.Expression<Func<FinancialObligation, FinancialObligationResponse>> MapResponse() =>
        o => new FinancialObligationResponse(
            o.IdFinancialObligation,
            o.NameObligation,
            o.IdCurrency,
            o.IdCurrencyNavigation.CodeCurrency,
            o.OriginalAmount,
            o.InterestRate,
            o.StartDate,
            o.TermMonths,
            o.IdBankAccountPayment,
            o.IdBankAccountPaymentNavigation != null ? o.IdBankAccountPaymentNavigation.CodeBankAccount : null,
            o.IdAccountLongTerm,
            o.IdAccountLongTermNavigation.CodeAccount,
            o.IdAccountShortTerm,
            o.IdAccountShortTermNavigation.CodeAccount,
            o.IdAccountInterest,
            o.IdAccountInterestNavigation.CodeAccount,
            o.IdAccountLateFee,
            o.IdAccountLateFeeNavigation != null ? o.IdAccountLateFeeNavigation.CodeAccount : null,
            o.IdAccountOther,
            o.IdAccountOtherNavigation != null ? o.IdAccountOtherNavigation.CodeAccount : null,
            o.MatchKeyword,
            o.StatusObligation,
            o.Notes,
            o.FinancialObligationInstallments.OrderBy(i => i.NumberInstallment).Select(i =>
                new FinancialObligationInstallmentResponse(
                    i.IdFinancialObligationInstallment,
                    i.NumberInstallment,
                    i.DueDate,
                    i.BalanceAfter,
                    i.AmountCapital,
                    i.AmountInterest,
                    i.AmountLateFee,
                    i.AmountOther,
                    i.AmountTotal,
                    i.StatusInstallment,
                    i.SyncedAt,
                    i.FinancialObligationPayment != null
                        ? new FinancialObligationPaymentResponse(
                            i.FinancialObligationPayment.IdFinancialObligationPayment,
                            i.FinancialObligationPayment.IdBankMovement,
                            i.FinancialObligationPayment.DatePayment,
                            i.FinancialObligationPayment.AmountPaid,
                            i.FinancialObligationPayment.AmountCapitalPaid,
                            i.FinancialObligationPayment.AmountInterestPaid,
                            i.FinancialObligationPayment.AmountLatePaid,
                            i.FinancialObligationPayment.AmountOtherPaid,
                            i.FinancialObligationPayment.IdAccountingEntry,
                            i.FinancialObligationPayment.IsAutoProcessed,
                            i.FinancialObligationPayment.Notes)
                        : null)).ToList());

    private static FinancialObligationPaymentResponse MapPayment(FinancialObligationPayment p) =>
        new(p.IdFinancialObligationPayment, p.IdBankMovement, p.DatePayment, p.AmountPaid,
            p.AmountCapitalPaid, p.AmountInterestPaid, p.AmountLatePaid, p.AmountOtherPaid,
            p.IdAccountingEntry, p.IsAutoProcessed, p.Notes);

    /// <summary>
    /// Busca el movimiento bancario BAC más apropiado para la cuota dada.
    /// Criterios: cuenta BAC configurada, monto ±1%, fecha ±10 días, descripción contiene MatchKeyword.
    /// </summary>
    private async Task<(int? BankMovementId, DateOnly? PaymentDate, string? Warning)> FindBankMovementAsync(
        FinancialObligation obligation,
        FinancialObligationInstallment installment,
        CancellationToken ct)
    {
        if (obligation.IdBankAccountPayment is null)
            return (null, null, null);

        var minDate = installment.DueDate.AddDays(-10);
        var maxDate = installment.DueDate.AddDays(10);
        var minAmt  = installment.AmountTotal * 0.99m;
        var maxAmt  = installment.AmountTotal * 1.01m;
        var keyword = obligation.MatchKeyword.ToUpper();

        // Excluir movimientos ya vinculados a otro pago
        var linkedIds = await db.FinancialObligationPayment
            .Where(p => p.IdBankMovement != null)
            .Select(p => p.IdBankMovement!.Value)
            .ToListAsync(ct);

        var candidates = await db.BankMovement
            .Where(bm =>
                bm.IdBankAccount == obligation.IdBankAccountPayment.Value &&
                bm.Amount >= minAmt && bm.Amount <= maxAmt &&
                bm.DateMovement >= minDate && bm.DateMovement <= maxDate &&
                bm.DescriptionMovement.ToUpper().Contains(keyword) &&
                !linkedIds.Contains(bm.IdBankMovement))
            .OrderBy(bm => Math.Abs((bm.DateMovement.DayNumber - installment.DueDate.DayNumber)))
            .ToListAsync(ct);

        if (candidates.Count == 0) return (null, null, null);

        string? warning = null;
        if (candidates.Count > 1)
            warning = $"Cuota {installment.NumberInstallment}: {candidates.Count} candidatos BAC encontrados, se usó el más cercano a la fecha de vencimiento.";

        var best = candidates[0];
        return (best.IdBankMovement, best.DateMovement, warning);
    }

    private static decimal CalculateShortTermPortion(FinancialObligation obligation, DateOnly asOfDate)
    {
        var horizon = asOfDate.AddYears(1);
        return obligation.FinancialObligationInstallments
            .Where(i => i.StatusInstallment is "Pendiente" or "Vigente"
                     && i.DueDate >= asOfDate
                     && i.DueDate <= horizon)
            .Sum(i => i.AmountCapital);
    }

    private async Task<decimal> GetLastReclassifiedPortionAsync(int idObligation, CancellationToken ct)
    {
        // Busca el último asiento de reclasificación generado para esta obligación
        var lastEntry = await db.AccountingEntry
            .Where(e => e.OriginModule == "FinancialObligationReclassify" && e.IdOriginRecord == idObligation)
            .OrderByDescending(e => e.DateEntry)
            .FirstOrDefaultAsync(ct);

        if (lastEntry is null) return 0;

        return await db.AccountingEntryLine
            .Where(l => l.IdAccountingEntry == lastEntry.IdAccountingEntry && l.DebitAmount > 0)
            .SumAsync(l => l.DebitAmount, ct);
    }

    private async Task<int> FindOrCreateFiscalPeriodIdAsync(DateOnly date, CancellationToken ct)
    {
        var period = await db.FiscalPeriod
            .FirstOrDefaultAsync(fp => fp.YearPeriod == date.Year && fp.MonthPeriod == date.Month, ct);

        if (period is not null) return period.IdFiscalPeriod;

        // Crear período automáticamente si no existe
        var months = new[] { "Enero","Febrero","Marzo","Abril","Mayo","Junio",
                              "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre" };
        var startOfMonth = new DateOnly(date.Year, date.Month, 1);
        var endOfMonth   = startOfMonth.AddMonths(1).AddDays(-1);

        period = new FiscalPeriod
        {
            YearPeriod   = date.Year,
            MonthPeriod  = date.Month,
            NamePeriod   = $"{months[date.Month - 1]} {date.Year}",
            StatusPeriod = "Abierto",
            StartDate    = startOfMonth,
            EndDate      = endOfMonth
        };
        db.FiscalPeriod.Add(period);
        await db.SaveChangesAsync(ct);
        return period.IdFiscalPeriod;
    }

    private static AccountingEntry BuildPaymentEntry(
        FinancialObligation obligation,
        FinancialObligationInstallment installment,
        DateOnly date,
        int idFiscalPeriod)
    {
        var lines = new List<AccountingEntryLine>();

        // DR Pasivo Corriente — capital
        lines.Add(new AccountingEntryLine
        {
            IdAccount       = obligation.IdAccountShortTerm,
            DebitAmount     = installment.AmountCapital,
            CreditAmount    = 0,
            DescriptionLine = $"Capital cuota {installment.NumberInstallment}"
        });

        // DR Gasto Intereses
        lines.Add(new AccountingEntryLine
        {
            IdAccount       = obligation.IdAccountInterest,
            DebitAmount     = installment.AmountInterest,
            CreditAmount    = 0,
            DescriptionLine = $"Intereses cuota {installment.NumberInstallment}"
        });

        // DR Gasto Mora (si hay)
        if (installment.AmountLateFee > 0)
        {
            var accountLateFee = obligation.IdAccountLateFee ?? obligation.IdAccountInterest;
            lines.Add(new AccountingEntryLine
            {
                IdAccount       = accountLateFee,
                DebitAmount     = installment.AmountLateFee,
                CreditAmount    = 0,
                DescriptionLine = $"Mora cuota {installment.NumberInstallment}"
            });
        }

        // DR Gasto Otros (si hay)
        if (installment.AmountOther > 0)
        {
            var accountOther = obligation.IdAccountOther ?? obligation.IdAccountInterest;
            lines.Add(new AccountingEntryLine
            {
                IdAccount       = accountOther,
                DebitAmount     = installment.AmountOther,
                CreditAmount    = 0,
                DescriptionLine = $"Otros cargos cuota {installment.NumberInstallment}"
            });
        }

        // CR Banco BAC — total pagado
        var bankAccountId = obligation.IdBankAccountPaymentNavigation?.IdAccount
                         ?? obligation.IdAccountShortTerm; // fallback si no hay cuenta BAC
        lines.Add(new AccountingEntryLine
        {
            IdAccount       = bankAccountId,
            DebitAmount     = 0,
            CreditAmount    = installment.AmountTotal,
            DescriptionLine = $"Pago cuota {installment.NumberInstallment} {obligation.MatchKeyword}"
        });

        return new AccountingEntry
        {
            IdFiscalPeriod   = idFiscalPeriod,
            IdCurrency       = obligation.IdCurrency,
            NumberEntry      = $"OBL-{obligation.IdFinancialObligation:D4}-{installment.NumberInstallment:D3}",
            DateEntry        = date,
            DescriptionEntry = $"Pago cuota {installment.NumberInstallment} — {obligation.NameObligation}",
            StatusEntry      = "Borrador",
            ExchangeRateValue = 1,
            OriginModule     = "FinancialObligation",
            IdOriginRecord   = obligation.IdFinancialObligation,
            AccountingEntryLines = lines
        };
    }

    private static AccountingEntry BuildPaymentEntryFromRequest(
        FinancialObligation obligation,
        FinancialObligationInstallment installment,
        RegisterPaymentRequest req,
        int idFiscalPeriod)
    {
        // Reutilizar la misma lógica con los montos del request
        var proxy = new FinancialObligationInstallment
        {
            IdFinancialObligation = installment.IdFinancialObligation,
            NumberInstallment     = installment.NumberInstallment,
            AmountCapital         = req.AmountCapitalPaid,
            AmountInterest        = req.AmountInterestPaid,
            AmountLateFee         = req.AmountLatePaid,
            AmountOther           = req.AmountOtherPaid,
            AmountTotal           = req.AmountPaid
        };
        return BuildPaymentEntry(obligation, proxy, req.DatePayment, idFiscalPeriod);
    }

    private static AccountingEntry BuildReclassificationEntry(
        FinancialObligation obligation,
        decimal portion,
        DateOnly date,
        int idFiscalPeriod) =>
        new()
        {
            IdFiscalPeriod   = idFiscalPeriod,
            IdCurrency       = obligation.IdCurrency,
            NumberEntry      = $"RCLS-{obligation.IdFinancialObligation:D4}-{date:yyyyMM}",
            DateEntry        = date,
            DescriptionEntry = $"Reclasificación porción corriente — {obligation.NameObligation}",
            StatusEntry      = "Borrador",
            ExchangeRateValue = 1,
            OriginModule     = "FinancialObligationReclassify",
            IdOriginRecord   = obligation.IdFinancialObligation,
            AccountingEntryLines =
            [
                new AccountingEntryLine
                {
                    IdAccount       = obligation.IdAccountLongTerm,
                    DebitAmount     = portion,
                    CreditAmount    = 0,
                    DescriptionLine = "Reclasificación a pasivo corriente"
                },
                new AccountingEntryLine
                {
                    IdAccount       = obligation.IdAccountShortTerm,
                    DebitAmount     = 0,
                    CreditAmount    = portion,
                    DescriptionLine = "Porción corriente 12 meses"
                }
            ]
        };

    private async Task ValidateAccountsAsync(
        int longTerm, int shortTerm, int interest,
        int? lateFee, int? other, CancellationToken ct)
    {
        var ids = new HashSet<int> { longTerm, shortTerm, interest };
        if (lateFee.HasValue) ids.Add(lateFee.Value);
        if (other.HasValue)   ids.Add(other.Value);

        var found = await db.Account.CountAsync(a => ids.Contains(a.IdAccount) && a.IsActive, ct);
        if (found != ids.Count)
            throw new InvalidOperationException("Una o más cuentas contables no existen o están inactivas.");
    }
}
