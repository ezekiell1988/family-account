using FamilyAccountApi.Features.FinancialStatements.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.FinancialStatements;

public sealed class FinancialStatementService(AppDbContext db) : IFinancialStatementService
{
    // ─── Tipos de cuenta por estado financiero ────────────────────────────────
    private static readonly string[] IncomeStatementTypes = ["Ingreso", "Gasto"];
    private static readonly string[] BalanceSheetTypes    = ["Activo", "Pasivo", "Capital"];
    private static readonly string[] AssetTypes           = ["Activo"];
    private static readonly string[] LiabilityTypes       = ["Pasivo"];
    private static readonly string[] EquityTypes          = ["Capital"];

    // ─── Registro interno de balance por cuenta ───────────────────────────────
    private sealed record AccountBalanceRow(
        int     IdAccount,
        string  CodeAccount,
        string  NameAccount,
        string  TypeAccount,
        int     LevelAccount,
        decimal TotalDebit,
        decimal TotalCredit);

    // ── Estado de Resultado ───────────────────────────────────────────────────

    public async Task<IncomeStatementResponse> GetIncomeStatementAsync(
        FinancialStatementFilterRequest filter,
        CancellationToken ct = default)
    {
        var (dateFrom, dateTo) = await ResolveDateRangeAsync(filter, ct);

        var lines = await QueryAccountBalancesAsync(
            dateFrom, dateTo,
            accumulativeMode: false,
            IncomeStatementTypes,
            ct);

        var revenues = lines
            .Where(l => l.TypeAccount == "Ingreso")
            .Select(l => new IncomeStatementLineResponse(
                l.IdAccount, l.CodeAccount, l.NameAccount, l.LevelAccount,
                l.TotalDebit, l.TotalCredit,
                Balance: l.TotalCredit - l.TotalDebit))   // Ingreso: saldo acreedor
            .OrderBy(l => l.CodeAccount)
            .ToList();

        var expenses = lines
            .Where(l => l.TypeAccount == "Gasto")
            .Select(l => new IncomeStatementLineResponse(
                l.IdAccount, l.CodeAccount, l.NameAccount, l.LevelAccount,
                l.TotalDebit, l.TotalCredit,
                Balance: l.TotalDebit - l.TotalCredit))   // Gasto: saldo deudor
            .OrderBy(l => l.CodeAccount)
            .ToList();

        var totalRevenues = revenues.Sum(r => r.Balance);
        var totalExpenses = expenses.Sum(e => e.Balance);

        return new IncomeStatementResponse(
            dateFrom,
            dateTo,
            revenues,
            expenses,
            totalRevenues,
            totalExpenses,
            NetIncome: totalRevenues - totalExpenses);
    }

    // ── Estado de Situación Financiera ────────────────────────────────────────

    public async Task<BalanceSheetResponse> GetBalanceSheetAsync(
        FinancialStatementFilterRequest filter,
        CancellationToken ct = default)
    {
        var (_, dateTo) = await ResolveDateRangeAsync(filter, ct);

        var lines = await QueryAccountBalancesAsync(
            dateFrom: DateOnly.MinValue,
            dateTo,
            accumulativeMode: true,       // acumula desde el origen hasta dateTo
            BalanceSheetTypes,
            ct);

        var assets = lines
            .Where(l => l.TypeAccount == "Activo")
            .Select(l => new BalanceSheetLineResponse(
                l.IdAccount, l.CodeAccount, l.NameAccount, l.LevelAccount,
                l.TotalDebit, l.TotalCredit,
                Balance: l.TotalDebit - l.TotalCredit))   // Activo: saldo deudor
            .OrderBy(l => l.CodeAccount)
            .ToList();

        var liabilities = lines
            .Where(l => l.TypeAccount == "Pasivo")
            .Select(l => new BalanceSheetLineResponse(
                l.IdAccount, l.CodeAccount, l.NameAccount, l.LevelAccount,
                l.TotalDebit, l.TotalCredit,
                Balance: l.TotalCredit - l.TotalDebit))   // Pasivo: saldo acreedor
            .OrderBy(l => l.CodeAccount)
            .ToList();

        var capital = lines
            .Where(l => l.TypeAccount == "Capital")
            .Select(l => new BalanceSheetLineResponse(
                l.IdAccount, l.CodeAccount, l.NameAccount, l.LevelAccount,
                l.TotalDebit, l.TotalCredit,
                Balance: l.TotalCredit - l.TotalDebit))   // Capital: saldo acreedor
            .OrderBy(l => l.CodeAccount)
            .ToList();

        var totalAssets       = assets.Sum(a => a.Balance);
        var totalLiabilities  = liabilities.Sum(l => l.Balance);
        var totalCapital      = capital.Sum(c => c.Balance);

        return new BalanceSheetResponse(
            dateTo,
            assets,
            liabilities,
            capital,
            totalAssets,
            totalLiabilities,
            totalCapital,
            TotalLiabilitiesAndCapital: totalLiabilities + totalCapital);
    }

    // ── Estado de Flujo de Efectivo ───────────────────────────────────────────

    public async Task<CashFlowStatementResponse> GetCashFlowStatementAsync(
        FinancialStatementFilterRequest filter,
        CancellationToken ct = default)
    {
        var (dateFrom, dateTo) = await ResolveDateRangeAsync(filter, ct);

        // Balances acumulados: antes del período (apertura) y al cierre
        var openingCutoff = dateFrom.AddDays(-1);
        var (openingAssets, closingAssets) = await QueryOpeningClosingAsync(openingCutoff, dateTo, AssetTypes, ct);
        var (openingLiab,   closingLiab)   = await QueryOpeningClosingAsync(openingCutoff, dateTo, LiabilityTypes, ct);
        var (openingEquity, closingEquity) = await QueryOpeningClosingAsync(openingCutoff, dateTo, EquityTypes, ct);

        // Movimientos del período (para PeriodDebits / PeriodCredits)
        var periodAll = await QueryAccountBalancesAsync(
            dateFrom, dateTo, accumulativeMode: false,
            BalanceSheetTypes, ct);

        var periodByAccount = periodAll.ToDictionary(r => r.IdAccount);

        CashFlowLineResponse ToLine(AccountBalanceRow closing, AccountBalanceRow? opening, bool creditNormal)
        {
            var openDebit  = opening?.TotalDebit  ?? 0m;
            var openCredit = opening?.TotalCredit ?? 0m;
            var openBal    = creditNormal ? openCredit - openDebit : openDebit - openCredit;

            var closeDebit  = closing.TotalDebit;
            var closeCredit = closing.TotalCredit;
            var closeBal    = creditNormal ? closeCredit - closeDebit : closeDebit - closeCredit;

            periodByAccount.TryGetValue(closing.IdAccount, out var period);
            var periodDeb = period?.TotalDebit  ?? 0m;
            var periodCre = period?.TotalCredit ?? 0m;

            return new CashFlowLineResponse(
                closing.IdAccount,
                closing.CodeAccount,
                closing.NameAccount,
                closing.LevelAccount,
                openBal,
                periodDeb,
                periodCre,
                closeBal,
                Change: closeBal - openBal);
        }

        var openingAssetDict  = openingAssets.ToDictionary(r => r.IdAccount);
        var openingLiabDict   = openingLiab.ToDictionary(r => r.IdAccount);
        var openingEquityDict = openingEquity.ToDictionary(r => r.IdAccount);

        var assetLines = closingAssets
            .Select(c => ToLine(c, openingAssetDict.GetValueOrDefault(c.IdAccount), creditNormal: false))
            .OrderBy(l => l.CodeAccount)
            .ToList();

        var liabilityLines = closingLiab
            .Select(c => ToLine(c, openingLiabDict.GetValueOrDefault(c.IdAccount), creditNormal: true))
            .OrderBy(l => l.CodeAccount)
            .ToList();

        var equityLines = closingEquity
            .Select(c => ToLine(c, openingEquityDict.GetValueOrDefault(c.IdAccount), creditNormal: true))
            .OrderBy(l => l.CodeAccount)
            .ToList();

        // Resultado del período (reutiliza la lógica del Estado de Resultado)
        var incomeData = await QueryAccountBalancesAsync(
            dateFrom, dateTo, accumulativeMode: false, IncomeStatementTypes, ct);
        var revenues = incomeData.Where(r => r.TypeAccount == "Ingreso").Sum(r => r.TotalCredit - r.TotalDebit);
        var expenses = incomeData.Where(r => r.TypeAccount == "Gasto").Sum(r => r.TotalDebit - r.TotalCredit);
        var netIncome = revenues - expenses;

        return new CashFlowStatementResponse(
            dateFrom,
            dateTo,
            netIncome,
            assetLines,
            liabilityLines,
            equityLines,
            TotalAssetChange:     assetLines.Sum(l => l.Change),
            TotalLiabilityChange: liabilityLines.Sum(l => l.Change),
            TotalEquityChange:    equityLines.Sum(l => l.Change));
    }

    // ── Estado de Cambios en el Patrimonio ────────────────────────────────────

    public async Task<EquityStatementResponse> GetEquityStatementAsync(
        FinancialStatementFilterRequest filter,
        CancellationToken ct = default)
    {
        var (dateFrom, dateTo) = await ResolveDateRangeAsync(filter, ct);

        var openingCutoff = dateFrom.AddDays(-1);
        var (openingEquity, closingEquity) = await QueryOpeningClosingAsync(openingCutoff, dateTo, EquityTypes, ct);

        // Movimientos del período para Capital (para separar créditos y débitos)
        var periodEquity = await QueryAccountBalancesAsync(
            dateFrom, dateTo, accumulativeMode: false, EquityTypes, ct);
        var periodByAccount = periodEquity.ToDictionary(r => r.IdAccount);

        var openingDict = openingEquity.ToDictionary(r => r.IdAccount);

        var movements = closingEquity.Select(closing =>
        {
            var opening = openingDict.GetValueOrDefault(closing.IdAccount);
            var openBal = (opening?.TotalCredit ?? 0m) - (opening?.TotalDebit ?? 0m);
            var closeBal = closing.TotalCredit - closing.TotalDebit;

            periodByAccount.TryGetValue(closing.IdAccount, out var period);

            return new EquityMovementResponse(
                closing.IdAccount,
                closing.CodeAccount,
                closing.NameAccount,
                closing.LevelAccount,
                OpeningBalance:  openBal,
                CreditsInPeriod: period?.TotalCredit ?? 0m,
                DebitsInPeriod:  period?.TotalDebit  ?? 0m,
                ClosingBalance:  closeBal);
        })
        .OrderBy(m => m.CodeAccount)
        .ToList();

        // Net Income del período
        var incomeData = await QueryAccountBalancesAsync(
            dateFrom, dateTo, accumulativeMode: false, IncomeStatementTypes, ct);
        var revenues  = incomeData.Where(r => r.TypeAccount == "Ingreso").Sum(r => r.TotalCredit - r.TotalDebit);
        var expenses  = incomeData.Where(r => r.TypeAccount == "Gasto").Sum(r => r.TotalDebit - r.TotalCredit);
        var netIncome = revenues - expenses;

        var totalOpening       = movements.Sum(m => m.OpeningBalance);
        var totalContributions = movements.Sum(m => m.CreditsInPeriod);
        var totalWithdrawals   = movements.Sum(m => m.DebitsInPeriod);
        var totalClosing       = movements.Sum(m => m.ClosingBalance);

        return new EquityStatementResponse(
            dateFrom,
            dateTo,
            netIncome,
            movements,
            totalOpening,
            totalContributions,
            totalWithdrawals,
            totalClosing,
            TotalEquityIncludingNetIncome: totalClosing + netIncome);
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private async Task<(DateOnly DateFrom, DateOnly DateTo)> ResolveDateRangeAsync(
        FinancialStatementFilterRequest filter,
        CancellationToken ct)
    {
        // 1. Período fiscal explícito
        if (filter.IdFiscalPeriod.HasValue)
        {
            var fp = await db.FiscalPeriod
                .AsNoTracking()
                .Where(f => f.IdFiscalPeriod == filter.IdFiscalPeriod.Value)
                .Select(f => new { f.StartDate, f.EndDate })
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException(
                    $"Período fiscal {filter.IdFiscalPeriod} no encontrado.");

            return (fp.StartDate, fp.EndDate);
        }

        // 2. Rango de fechas directo
        if (filter.DateFrom.HasValue && filter.DateTo.HasValue)
            return (filter.DateFrom.Value, filter.DateTo.Value);

        // 3. Año + mes → mes específico
        if (filter.Year.HasValue && filter.Month.HasValue)
        {
            var from = new DateOnly(filter.Year.Value, filter.Month.Value, 1);
            var to   = from.AddMonths(1).AddDays(-1);
            return (from, to);
        }

        // 4. Solo año → año completo
        if (filter.Year.HasValue)
            return (new DateOnly(filter.Year.Value, 1, 1),
                    new DateOnly(filter.Year.Value, 12, 31));

        throw new ArgumentException(
            "Debe especificar al menos una de las siguientes combinaciones: " +
            "idFiscalPeriod | dateFrom+dateTo | year+month | year.");
    }

    /// <summary>
    /// Devuelve balances acumulativos de apertura (hasta openingCutoff) y
    /// cierre (hasta closingCutoff) para los tipos de cuenta indicados.
    /// </summary>
    private async Task<(List<AccountBalanceRow> Opening, List<AccountBalanceRow> Closing)>
        QueryOpeningClosingAsync(
            DateOnly openingCutoff,
            DateOnly closingCutoff,
            string[] accountTypes,
            CancellationToken ct)
    {
        var opening = await QueryAccountBalancesAsync(
            DateOnly.MinValue, openingCutoff, accumulativeMode: true, accountTypes, ct);
        var closing = await QueryAccountBalancesAsync(
            DateOnly.MinValue, closingCutoff, accumulativeMode: true, accountTypes, ct);
        return (opening, closing);
    }

    /// <summary>
    /// Consulta saldos por cuenta sumando débito y crédito de asientos publicados.
    /// accumulativeMode=true ignora dateFrom (acumula desde el origen hasta dateTo).
    /// </summary>
    private Task<List<AccountBalanceRow>> QueryAccountBalancesAsync(
        DateOnly dateFrom,
        DateOnly dateTo,
        bool accumulativeMode,
        string[] accountTypes,
        CancellationToken ct)
    {
        var baseQuery = db.AccountingEntryLine
            .Join(db.AccountingEntry,
                ael => ael.IdAccountingEntry,
                ae  => ae.IdAccountingEntry,
                (ael, ae) => new { ael, ae })
            .Join(db.Account,
                x => x.ael.IdAccount,
                a  => a.IdAccount,
                (x, a) => new { x.ael, x.ae, a })
            .Where(x => x.ae.StatusEntry == "Publicado"
                        && x.ae.DateEntry <= dateTo
                        && accountTypes.Contains(x.a.TypeAccount));

        // El modo acumulativo no aplica límite inferior (balance de situación financiera).
        if (!accumulativeMode)
            baseQuery = baseQuery.Where(x => x.ae.DateEntry >= dateFrom);

        return baseQuery
            .GroupBy(x => new
            {
                x.a.IdAccount,
                x.a.CodeAccount,
                x.a.NameAccount,
                x.a.TypeAccount,
                x.a.LevelAccount
            })
            .Select(g => new AccountBalanceRow(
                g.Key.IdAccount,
                g.Key.CodeAccount,
                g.Key.NameAccount,
                g.Key.TypeAccount,
                g.Key.LevelAccount,
                g.Sum(x => x.ael.DebitAmount),
                g.Sum(x => x.ael.CreditAmount)))
            .ToListAsync(ct);
    }
}
