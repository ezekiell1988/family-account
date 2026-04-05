using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.SalesInvoices.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.SalesInvoices;

public sealed class SalesInvoiceService(AppDbContext db) : ISalesInvoiceService
{
    // ── Proyección compartida ─────────────────────────────────────────────────
    private IQueryable<SalesInvoice> BuildQuery() =>
        db.SalesInvoice
            .AsNoTracking()
            .Include(si => si.IdFiscalPeriodNavigation)
            .Include(si => si.IdCurrencyNavigation)
            .Include(si => si.IdSalesInvoiceTypeNavigation)
            .Include(si => si.IdContactNavigation)
            .Include(si => si.IdBankAccountNavigation)
            .Include(si => si.SalesInvoiceLines)
                .ThenInclude(l => l.IdProductNavigation)
            .Include(si => si.SalesInvoiceLines)
                .ThenInclude(l => l.IdUnitNavigation)
            .Include(si => si.SalesInvoiceLines)
                .ThenInclude(l => l.IdInventoryLotNavigation)
            .Include(si => si.SalesInvoiceEntries);

    private static SalesInvoiceResponse ToResponse(SalesInvoice si) => new(
        si.IdSalesInvoice,
        si.IdFiscalPeriod,
        si.IdFiscalPeriodNavigation.NamePeriod,
        si.IdCurrency,
        si.IdCurrencyNavigation.CodeCurrency,
        si.IdCurrencyNavigation.NameCurrency,
        si.IdSalesInvoiceType,
        si.IdSalesInvoiceTypeNavigation.CodeSalesInvoiceType,
        si.IdSalesInvoiceTypeNavigation.NameSalesInvoiceType,
        si.IdSalesInvoiceTypeNavigation.CounterpartFromBankMovement,
        si.IdContact,
        si.IdContactNavigation?.Name,
        si.IdBankAccount,
        si.IdBankAccountNavigation?.CodeBankAccount,
        si.NumberInvoice,
        si.DateInvoice,
        si.SubTotalAmount,
        si.TaxAmount,
        si.TotalAmount,
        si.StatusInvoice,
        si.DescriptionInvoice,
        si.ExchangeRateValue,
        si.CreatedAt,
        si.SalesInvoiceEntries.FirstOrDefault()?.IdAccountingEntry,
        si.SalesInvoiceLines
            .Select(l => new SalesInvoiceLineResponse(
                l.IdSalesInvoiceLine,
                l.IdSalesInvoice,
                l.IdProduct,
                l.IdProductNavigation?.NameProduct,
                l.IdUnit,
                l.IdUnitNavigation?.CodeUnit,
                l.IdInventoryLot,
                l.IdInventoryLotNavigation?.LotNumber,
                l.DescriptionLine,
                l.Quantity,
                l.QuantityBase ?? l.Quantity,
                l.UnitPrice,
                l.UnitCost,
                l.TaxPercent,
                l.TotalLineAmount))
            .ToList());

    // ── Consultas ─────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<SalesInvoiceResponse>> GetAllAsync(CancellationToken ct = default) =>
        (await BuildQuery()
            .OrderByDescending(si => si.DateInvoice)
            .ThenByDescending(si => si.IdSalesInvoice)
            .ToListAsync(ct))
            .Select(ToResponse).ToList();

    public async Task<IReadOnlyList<SalesInvoiceResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default) =>
        (await BuildQuery()
            .Where(si => si.IdFiscalPeriod == idFiscalPeriod)
            .OrderByDescending(si => si.DateInvoice)
            .ThenByDescending(si => si.IdSalesInvoice)
            .ToListAsync(ct))
            .Select(ToResponse).ToList();

    public async Task<SalesInvoiceResponse?> GetByIdAsync(int idSalesInvoice, CancellationToken ct = default)
    {
        var entity = await BuildQuery()
            .FirstOrDefaultAsync(si => si.IdSalesInvoice == idSalesInvoice, ct);
        return entity is null ? null : ToResponse(entity);
    }

    // ── CREAR ─────────────────────────────────────────────────────────────────
    public async Task<SalesInvoiceResponse> CreateAsync(CreateSalesInvoiceRequest request, CancellationToken ct = default)
    {
        var entity = new SalesInvoice
        {
            IdFiscalPeriod      = request.IdFiscalPeriod,
            IdCurrency          = request.IdCurrency,
            IdSalesInvoiceType  = request.IdSalesInvoiceType,
            IdContact           = request.IdContact,
            IdBankAccount       = request.IdBankAccount,
            NumberInvoice       = string.Empty,
            DateInvoice         = request.DateInvoice,
            SubTotalAmount      = request.SubTotalAmount,
            TaxAmount           = request.TaxAmount,
            TotalAmount         = request.TotalAmount,
            StatusInvoice       = "Borrador",
            DescriptionInvoice  = string.IsNullOrWhiteSpace(request.DescriptionInvoice) ? null : request.DescriptionInvoice.Trim(),
            ExchangeRateValue   = request.ExchangeRateValue,
            CreatedAt           = DateTime.UtcNow,
        };

        var (mappedLines, linesError) = await MapLinesAsync(request.Lines, ct);
        if (linesError is not null)
            throw new InvalidOperationException(linesError);

        entity.SalesInvoiceLines = mappedLines;

        db.SalesInvoice.Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);

        return (await GetByIdAsync(entity.IdSalesInvoice, ct))!;
    }

    // ── ACTUALIZAR ────────────────────────────────────────────────────────────
    public async Task<SalesInvoiceResponse?> UpdateAsync(int idSalesInvoice, UpdateSalesInvoiceRequest request, CancellationToken ct = default)
    {
        var entity = await db.SalesInvoice
            .Include(si => si.SalesInvoiceLines)
            .FirstOrDefaultAsync(si => si.IdSalesInvoice == idSalesInvoice, ct);

        if (entity is null) return null;

        if (entity.StatusInvoice != "Borrador")
            throw new InvalidOperationException($"Solo se pueden modificar facturas en estado 'Borrador'. Estado actual: '{entity.StatusInvoice}'.");

        entity.IdFiscalPeriod     = request.IdFiscalPeriod;
        entity.IdCurrency         = request.IdCurrency;
        entity.IdSalesInvoiceType = request.IdSalesInvoiceType;
        entity.IdContact          = request.IdContact;
        entity.IdBankAccount      = request.IdBankAccount;
        entity.DateInvoice        = request.DateInvoice;
        entity.SubTotalAmount     = request.SubTotalAmount;
        entity.TaxAmount          = request.TaxAmount;
        entity.TotalAmount        = request.TotalAmount;
        entity.DescriptionInvoice = string.IsNullOrWhiteSpace(request.DescriptionInvoice) ? null : request.DescriptionInvoice.Trim();
        entity.ExchangeRateValue  = request.ExchangeRateValue;

        var (mappedLines, linesError) = await MapLinesAsync(request.Lines, ct);
        if (linesError is not null)
            throw new InvalidOperationException(linesError);

        db.SalesInvoiceLine.RemoveRange(entity.SalesInvoiceLines);
        entity.SalesInvoiceLines.Clear();

        foreach (var line in mappedLines)
            entity.SalesInvoiceLines.Add(line);

        await db.SaveChangesAsync(CancellationToken.None);

        return await GetByIdAsync(idSalesInvoice, ct);
    }

    // ── ELIMINAR ──────────────────────────────────────────────────────────────
    public async Task<bool> DeleteAsync(int idSalesInvoice, CancellationToken ct = default)
    {
        var entity = await db.SalesInvoice
            .AsNoTracking()
            .FirstOrDefaultAsync(si => si.IdSalesInvoice == idSalesInvoice, ct);

        if (entity is null) return false;
        if (entity.StatusInvoice != "Borrador")
            throw new InvalidOperationException("Solo se pueden eliminar facturas en estado 'Borrador'.");

        var deleted = await db.SalesInvoice
            .Where(si => si.IdSalesInvoice == idSalesInvoice)
            .ExecuteDeleteAsync(CancellationToken.None);

        return deleted > 0;
    }

    // ── CONFIRMAR ─────────────────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, SalesInvoiceResponse? Invoice)> ConfirmAsync(
        int idSalesInvoice, CancellationToken ct = default)
    {
        var invoice = await db.SalesInvoice
            .Include(si => si.IdSalesInvoiceTypeNavigation)
                .ThenInclude(sit => sit.IdBankMovementTypeNavigation)
            .Include(si => si.IdCurrencyNavigation)
            .Include(si => si.SalesInvoiceLines)
            .Include(si => si.BankMovementDocuments)
                .ThenInclude(doc => doc.IdBankMovementNavigation)
                    .ThenInclude(bm => bm.IdBankAccountNavigation)
            .FirstOrDefaultAsync(si => si.IdSalesInvoice == idSalesInvoice, ct);

        if (invoice is null)
            return (false, "Factura de venta no encontrada.", null);

        if (invoice.StatusInvoice != "Borrador")
            return (false, $"Solo se pueden confirmar facturas en estado 'Borrador'. Estado actual: '{invoice.StatusInvoice}'.", null);

        var invoiceType = invoice.IdSalesInvoiceTypeNavigation;

        // ── 1. Resolver cuenta DR (contrapartida de la venta: Caja o Banco) ──
        int? drAccountId;
        if (!invoiceType.CounterpartFromBankMovement)
        {
            bool isUsd = invoice.IdCurrencyNavigation.CodeCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase);
            drAccountId = isUsd ? invoiceType.IdAccountCounterpartUSD : invoiceType.IdAccountCounterpartCRC;

            if (drAccountId is null)
                return (false,
                    $"El tipo de factura '{invoiceType.CodeSalesInvoiceType}' no tiene configurada la cuenta Caja para la moneda '{invoice.IdCurrencyNavigation.CodeCurrency}'. Configure IdAccountCounterpartCRC o IdAccountCounterpartUSD en el tipo.",
                    null);
        }
        else
        {
            var bankDoc = invoice.BankMovementDocuments.FirstOrDefault();
            if (bankDoc is null)
            {
                if (invoice.IdBankAccount is null)
                    return (false,
                        $"La factura de tipo '{invoiceType.CodeSalesInvoiceType}' requiere una cuenta bancaria (IdBankAccount) para crear automáticamente el movimiento bancario.",
                        null);

                if (invoiceType.IdBankMovementType is null || invoiceType.IdBankMovementTypeNavigation is null)
                    return (false,
                        $"El tipo de factura '{invoiceType.CodeSalesInvoiceType}' no tiene configurado el tipo de movimiento bancario (IdBankMovementType).",
                        null);

                var bankAccount = await db.BankAccount
                    .Include(ba => ba.IdCurrencyNavigation)
                    .FirstOrDefaultAsync(ba => ba.IdBankAccount == invoice.IdBankAccount, ct);

                if (bankAccount is null)
                    return (false, "La cuenta bancaria vinculada a la factura no existe.", null);

                var movType = invoiceType.IdBankMovementTypeNavigation;
                var isAbono = movType.MovementSign == "Abono";   // venta → cobro → Abono en cuenta corriente

                var bkEntry = new AccountingEntry
                {
                    IdFiscalPeriod    = invoice.IdFiscalPeriod,
                    IdCurrency        = bankAccount.IdCurrency,
                    NumberEntry       = $"BK-FV-{invoice.IdSalesInvoice:D6}",
                    DateEntry         = invoice.DateInvoice,
                    DescriptionEntry  = $"Movimiento bancario — Venta {invoice.IdSalesInvoice:D6}",
                    StatusEntry       = "Publicado",
                    ExchangeRateValue = invoice.ExchangeRateValue,
                    OriginModule      = "BankMovement",
                    CreatedAt         = DateTime.UtcNow,
                    AccountingEntryLines =
                    [
                        new AccountingEntryLine
                        {
                            IdAccount       = isAbono ? movType.IdAccountCounterpart : bankAccount.IdAccount,
                            DebitAmount     = invoice.TotalAmount,
                            CreditAmount    = 0,
                            DescriptionLine = $"Cobro venta — {invoice.IdSalesInvoice:D6}"
                        },
                        new AccountingEntryLine
                        {
                            IdAccount       = isAbono ? bankAccount.IdAccount : movType.IdAccountCounterpart,
                            DebitAmount     = 0,
                            CreditAmount    = invoice.TotalAmount,
                            DescriptionLine = $"Cobro venta — {invoice.IdSalesInvoice:D6}"
                        },
                    ]
                };

                db.AccountingEntry.Add(bkEntry);
                await db.SaveChangesAsync(CancellationToken.None);

                var newMovement = new BankMovement
                {
                    IdBankAccount       = invoice.IdBankAccount.Value,
                    IdBankMovementType  = invoiceType.IdBankMovementType.Value,
                    IdFiscalPeriod      = invoice.IdFiscalPeriod,
                    NumberMovement      = $"FV-{invoice.IdSalesInvoice:D6}",
                    DateMovement        = invoice.DateInvoice,
                    DescriptionMovement = $"Venta {invoice.IdSalesInvoice:D6}",
                    Amount              = invoice.TotalAmount,
                    StatusMovement      = "Confirmado",
                    ExchangeRateValue   = invoice.ExchangeRateValue,
                    CreatedAt           = DateTime.UtcNow,
                    IdAccountingEntry   = bkEntry.IdAccountingEntry
                };

                db.BankMovement.Add(newMovement);
                await db.SaveChangesAsync(CancellationToken.None);

                var newDoc = new BankMovementDocument
                {
                    IdBankMovement      = newMovement.IdBankMovement,
                    IdSalesInvoice      = invoice.IdSalesInvoice,
                    TypeDocument        = "FacturaVenta",
                    NumberDocument      = invoice.IdSalesInvoice.ToString("D6"),
                    DateDocument        = invoice.DateInvoice,
                    AmountDocument      = invoice.TotalAmount,
                    DescriptionDocument = $"Venta {invoice.IdSalesInvoice:D6}"
                };

                db.BankMovementDocument.Add(newDoc);
                await db.SaveChangesAsync(CancellationToken.None);

                drAccountId = bankAccount.IdAccount;
            }
            else
            {
                drAccountId = bankDoc.IdBankMovementNavigation.IdBankAccountNavigation.IdAccount;
            }
        }

        // ── 2. Construir líneas CR de ingresos (por ProductAccount o fallback) ──
        var crLines = new List<(AccountingEntryLine Line, SalesInvoiceLine SourceLine)>();

        foreach (var invoiceLine in invoice.SalesInvoiceLines)
        {
            if (invoiceLine.IdProduct is not null)
            {
                var productAccounts = await db.ProductAccount
                    .AsNoTracking()
                    .Where(pa => pa.IdProduct == invoiceLine.IdProduct)
                    .ToListAsync(ct);

                if (productAccounts.Count > 0)
                {
                    foreach (var pa in productAccounts)
                    {
                        var rawAmount = Math.Round(invoiceLine.TotalLineAmount * pa.PercentageAccount / 100m, 2);
                        var absAmount = Math.Abs(rawAmount);
                        crLines.Add((new AccountingEntryLine
                        {
                            IdAccount       = pa.IdAccount,
                            DebitAmount     = rawAmount <  0 ? absAmount : 0,
                            CreditAmount    = rawAmount >= 0 ? absAmount : 0,
                            DescriptionLine = invoiceLine.DescriptionLine,
                            IdCostCenter    = pa.IdCostCenter
                        }, invoiceLine));
                    }
                    continue;
                }
            }

            if (invoiceType.IdAccountSalesRevenue is not null)
            {
                crLines.Add((new AccountingEntryLine
                {
                    IdAccount       = invoiceType.IdAccountSalesRevenue.Value,
                    DebitAmount     = 0,
                    CreditAmount    = Math.Round(invoiceLine.TotalLineAmount, 2),
                    DescriptionLine = invoiceLine.DescriptionLine,
                }, invoiceLine));
            }
        }

        if (crLines.Count == 0)
            return (false,
                "No se pudo generar el asiento contable. Configure una cuenta de ingresos fallback en el tipo de factura (IdAccountSalesRevenue), o asigne distribución contable (ProductAccount) a los productos de las líneas.",
                null);

        var totalCr = crLines.Sum(x => x.Line.CreditAmount - x.Line.DebitAmount);

        if (totalCr <= 0)
            return (false,
                $"El total neto del asiento es {totalCr:N2}. Los porcentajes de distribución deben resultar en un crédito neto positivo.",
                null);

        // Línea DR (contrapartida — banco/caja)
        var drLine = new AccountingEntryLine
        {
            IdAccount       = drAccountId.Value,
            DebitAmount     = totalCr,
            CreditAmount    = 0,
            DescriptionLine = $"Cobro venta — {invoice.IdSalesInvoice:D6}"
        };

        // ── 3. Crear asiento de ingresos (OriginModule = "SalesInvoice") ──────
        var prefix = $"FV-{invoice.DateInvoice:yyyyMMdd}-";
        var seq = await db.SalesInvoice.CountAsync(
            si => si.StatusInvoice == "Confirmado" && EF.Functions.Like(si.NumberInvoice, prefix + "%"),
            CancellationToken.None);

        var entry = new AccountingEntry
        {
            IdFiscalPeriod    = invoice.IdFiscalPeriod,
            IdCurrency        = invoice.IdCurrency,
            NumberEntry       = $"{prefix}{seq + 1:D3}",
            DateEntry         = invoice.DateInvoice,
            DescriptionEntry  = $"Factura de venta {prefix}{seq + 1:D3}",
            StatusEntry       = "Publicado",
            ExchangeRateValue = invoice.ExchangeRateValue,
            OriginModule      = "SalesInvoice",
            IdOriginRecord    = invoice.IdSalesInvoice,
            CreatedAt         = DateTime.UtcNow
        };

        entry.AccountingEntryLines.Add(drLine);
        foreach (var (line, _) in crLines) entry.AccountingEntryLines.Add(line);

        db.AccountingEntry.Add(entry);
        await db.SaveChangesAsync(CancellationToken.None);

        // Vincular asiento a la factura
        db.SalesInvoiceEntry.Add(new SalesInvoiceEntry
        {
            IdSalesInvoice    = invoice.IdSalesInvoice,
            IdAccountingEntry = entry.IdAccountingEntry
        });

        // Vincular líneas contables a líneas de factura
        foreach (var (line, sourceLine) in crLines)
        {
            db.SalesInvoiceLineEntry.Add(new SalesInvoiceLineEntry
            {
                IdSalesInvoiceLine    = sourceLine.IdSalesInvoiceLine,
                IdAccountingEntryLine = line.IdAccountingEntryLine
            });
        }

        await db.SaveChangesAsync(CancellationToken.None);

        // ── 4. Actualizar estado + NumberInvoice ─────────────────────────────
        var invoiceToUpdate = await db.SalesInvoice.FindAsync([invoice.IdSalesInvoice], ct);
        invoiceToUpdate!.StatusInvoice = "Confirmado";
        invoiceToUpdate!.NumberInvoice = $"{prefix}{seq + 1:D3}";

        await db.SaveChangesAsync(CancellationToken.None);

        // ── 5. Decrementar lotes y registrar COGS ────────────────────────────
        var cogsLines  = new List<(AccountingEntryLine Line, SalesInvoiceLine SourceLine)>();
        var totalCogsAmount = 0m;

        foreach (var invoiceLine in invoice.SalesInvoiceLines)
        {
            if (invoiceLine.IdProduct is null || invoiceLine.IdInventoryLot is null) continue;

            var lot = await db.InventoryLot.FindAsync([invoiceLine.IdInventoryLot.Value], CancellationToken.None);
            if (lot is null) continue;

            var pu = await db.ProductUnit
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.IdProduct == invoiceLine.IdProduct.Value && u.IdUnit == (invoiceLine.IdUnit ?? invoiceLine.IdProduct),
                    CancellationToken.None);

            decimal conversionFactor = pu?.ConversionFactor ?? 1m;
            decimal qtyBase          = Math.Round(invoiceLine.Quantity * conversionFactor, 6);
            decimal unitCost         = lot.UnitCost;
            decimal lineCogsAmount   = Math.Round(qtyBase * unitCost, 2);

            // Actualizar línea
            var trackedLine = await db.SalesInvoiceLine.FindAsync([invoiceLine.IdSalesInvoiceLine], CancellationToken.None);
            if (trackedLine is not null)
            {
                trackedLine.QuantityBase = qtyBase;
                trackedLine.UnitCost     = unitCost;
            }

            // Decrementar lote
            lot.QuantityAvailable -= qtyBase;
            if (lot.QuantityAvailable < 0)
                return (false,
                    $"El lote '{lot.LotNumber ?? lot.IdInventoryLot.ToString()}' del producto {invoiceLine.IdProduct} quedaría con stock negativo ({lot.QuantityAvailable:N4}) al decrementar {qtyBase:N4} unidades.",
                    null);

            await db.SaveChangesAsync(CancellationToken.None);

            // Recalcular AverageCost
            var product = await db.Product.FindAsync([invoiceLine.IdProduct.Value], CancellationToken.None);
            var allLots = await db.InventoryLot
                .Where(il => il.IdProduct == invoiceLine.IdProduct.Value)
                .ToListAsync(CancellationToken.None);

            var totalQty  = allLots.Sum(il => il.QuantityAvailable);
            var totalCost = allLots.Sum(il => il.QuantityAvailable * il.UnitCost);
            if (product is not null)
                product.AverageCost = totalQty > 0 ? Math.Round(totalCost / totalQty, 6) : 0m;

            totalCogsAmount += lineCogsAmount;
            cogsLines.Add((new AccountingEntryLine
            {
                IdAccount       = invoiceType.IdAccountCOGS    ?? 0,
                DebitAmount     = lineCogsAmount,
                CreditAmount    = 0,
                DescriptionLine = invoiceLine.DescriptionLine
            }, invoiceLine));
        }

        await db.SaveChangesAsync(CancellationToken.None);

        // ── 6. Asiento de COGS (solo si hay cuentas configuradas y líneas) ───
        if (cogsLines.Count > 0 && invoiceType.IdAccountCOGS is not null && invoiceType.IdAccountInventory is not null)
        {
            var cogsEntry = new AccountingEntry
            {
                IdFiscalPeriod    = invoice.IdFiscalPeriod,
                IdCurrency        = invoice.IdCurrency,
                NumberEntry       = $"COGS-FV-{invoice.IdSalesInvoice:D6}",
                DateEntry         = invoice.DateInvoice,
                DescriptionEntry  = $"Costo de ventas — {prefix}{seq + 1:D3}",
                StatusEntry       = "Publicado",
                ExchangeRateValue = invoice.ExchangeRateValue,
                OriginModule      = "SalesInvoice-COGS",
                IdOriginRecord    = invoice.IdSalesInvoice,
                CreatedAt         = DateTime.UtcNow,
            };

            foreach (var (line, _) in cogsLines) cogsEntry.AccountingEntryLines.Add(line);

            // Línea CR inventario (suma total)
            cogsEntry.AccountingEntryLines.Add(new AccountingEntryLine
            {
                IdAccount       = invoiceType.IdAccountInventory.Value,
                DebitAmount     = 0,
                CreditAmount    = totalCogsAmount,
                DescriptionLine = $"Salida inventario — {prefix}{seq + 1:D3}"
            });

            db.AccountingEntry.Add(cogsEntry);
            await db.SaveChangesAsync(CancellationToken.None);

            db.SalesInvoiceEntry.Add(new SalesInvoiceEntry
            {
                IdSalesInvoice    = invoice.IdSalesInvoice,
                IdAccountingEntry = cogsEntry.IdAccountingEntry
            });

            foreach (var (line, sourceLine) in cogsLines)
            {
                db.SalesInvoiceLineEntry.Add(new SalesInvoiceLineEntry
                {
                    IdSalesInvoiceLine    = sourceLine.IdSalesInvoiceLine,
                    IdAccountingEntryLine = line.IdAccountingEntryLine
                });
            }

            await db.SaveChangesAsync(CancellationToken.None);
        }

        return (true, null, await GetByIdAsync(invoice.IdSalesInvoice, ct));
    }

    // ── ANULAR ────────────────────────────────────────────────────────────────
    public async Task<(SalesInvoiceResponse? Result, string? ConflictMessage)> CancelAsync(
        int idSalesInvoice, CancellationToken ct = default)
    {
        var invoice = await db.SalesInvoice
            .Include(si => si.SalesInvoiceLines)
            .Include(si => si.BankMovementDocuments)
            .FirstOrDefaultAsync(si => si.IdSalesInvoice == idSalesInvoice, ct);

        if (invoice is null) return (null, null);

        if (invoice.StatusInvoice == "Anulado")
            return (await GetByIdAsync(idSalesInvoice, ct), null);

        if (invoice.StatusInvoice != "Confirmado")
            return (null, $"Solo se pueden anular facturas en estado 'Confirmado'. Estado actual: '{invoice.StatusInvoice}'.");

        // ── Revertir lotes e inventario ───────────────────────────────────────
        foreach (var line in invoice.SalesInvoiceLines)
        {
            if (line.IdInventoryLot is null || line.QuantityBase is null) continue;

            var lot = await db.InventoryLot.FindAsync([line.IdInventoryLot.Value], CancellationToken.None);
            if (lot is null) continue;

            lot.QuantityAvailable += line.QuantityBase.Value;

            await db.SaveChangesAsync(CancellationToken.None);

            // Recalcular AverageCost
            if (line.IdProduct is not null)
            {
                var product = await db.Product.FindAsync([line.IdProduct.Value], CancellationToken.None);
                var allLots = await db.InventoryLot
                    .Where(il => il.IdProduct == line.IdProduct.Value)
                    .ToListAsync(CancellationToken.None);

                var totalQty  = allLots.Sum(il => il.QuantityAvailable);
                var totalCost = allLots.Sum(il => il.QuantityAvailable * il.UnitCost);
                if (product is not null)
                    product.AverageCost = totalQty > 0 ? Math.Round(totalCost / totalQty, 6) : 0m;
            }
        }

        // ── Anular asientos contables ────────────────────────────────────────
        var entryIds = await db.SalesInvoiceEntry
            .Where(sie => sie.IdSalesInvoice == idSalesInvoice)
            .Select(sie => sie.IdAccountingEntry)
            .ToListAsync(CancellationToken.None);

        if (entryIds.Count > 0)
            await db.AccountingEntry
                .Where(ae => entryIds.Contains(ae.IdAccountingEntry) && ae.StatusEntry != "Anulado")
                .ExecuteUpdateAsync(s => s.SetProperty(ae => ae.StatusEntry, "Anulado"), CancellationToken.None);

        // ── Anular movimiento bancario auto-creado ────────────────────────────
        var salesDoc = invoice.BankMovementDocuments
            .FirstOrDefault(d => d.TypeDocument == "FacturaVenta");

        if (salesDoc is not null)
        {
            await db.BankMovement
                .Where(bm => bm.IdBankMovement == salesDoc.IdBankMovement && bm.StatusMovement != "Anulado")
                .ExecuteUpdateAsync(s => s.SetProperty(bm => bm.StatusMovement, "Anulado"), CancellationToken.None);
        }

        // ── Cambiar estado ────────────────────────────────────────────────────
        invoice.StatusInvoice = "Anulado";
        await db.SaveChangesAsync(CancellationToken.None);

        return (await GetByIdAsync(idSalesInvoice, ct), null);
    }

    // ── Helpers privados ──────────────────────────────────────────────────────
    private async Task<(List<SalesInvoiceLine> Lines, string? Error)> MapLinesAsync(
        IReadOnlyList<SalesInvoiceLineRequest> lines, CancellationToken ct)
    {
        var result = new List<SalesInvoiceLine>(lines.Count);

        foreach (var l in lines)
        {
            decimal? qtyBase = null;

            if (l.IdProduct.HasValue && l.IdUnit.HasValue)
            {
                var pu = await db.ProductUnit
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.IdProduct == l.IdProduct.Value && p.IdUnit == l.IdUnit.Value,
                        ct);

                if (pu is null)
                    return ([], $"No existe una presentación (ProductUnit) para el producto {l.IdProduct} con la unidad {l.IdUnit}. Configure la presentación antes de registrar la línea.");

                qtyBase = Math.Round(l.Quantity * pu.ConversionFactor, 6);
            }

            result.Add(new SalesInvoiceLine
            {
                IdProduct       = l.IdProduct,
                IdUnit          = l.IdUnit,
                IdInventoryLot  = l.IdInventoryLot,
                DescriptionLine = l.DescriptionLine.Trim(),
                Quantity        = l.Quantity,
                QuantityBase    = qtyBase,
                UnitPrice       = l.UnitPrice,
                TaxPercent      = l.TaxPercent,
                TotalLineAmount = l.TotalLineAmount
            });
        }

        return (result, null);
    }
}
