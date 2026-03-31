using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.PurchaseInvoices.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.PurchaseInvoices;

public sealed class PurchaseInvoiceService(AppDbContext db) : IPurchaseInvoiceService
{
    // ── Proyección compartida ─────────────────────────────────────────────────
    private IQueryable<PurchaseInvoice> BuildQuery() =>
        db.PurchaseInvoice
            .AsNoTracking()
            .Include(pi => pi.IdFiscalPeriodNavigation)
            .Include(pi => pi.IdCurrencyNavigation)
            .Include(pi => pi.IdPurchaseInvoiceTypeNavigation)
            .Include(pi => pi.IdBankAccountNavigation)
            .Include(pi => pi.PurchaseInvoiceLines)
                .ThenInclude(l => l.IdProductSKUNavigation)
            .Include(pi => pi.PurchaseInvoiceEntries);

    private static PurchaseInvoiceResponse ToResponse(PurchaseInvoice pi) => new(
        pi.IdPurchaseInvoice,
        pi.IdFiscalPeriod,
        pi.IdFiscalPeriodNavigation.NamePeriod,
        pi.IdCurrency,
        pi.IdCurrencyNavigation.CodeCurrency,
        pi.IdCurrencyNavigation.NameCurrency,
        pi.IdPurchaseInvoiceType,
        pi.IdPurchaseInvoiceTypeNavigation.CodePurchaseInvoiceType,
        pi.IdPurchaseInvoiceTypeNavigation.NamePurchaseInvoiceType,
        pi.IdPurchaseInvoiceTypeNavigation.CounterpartFromBankMovement,
        pi.IdBankAccount,
        pi.IdBankAccountNavigation?.CodeBankAccount,
        pi.NumberInvoice,
        pi.ProviderName,
        pi.DateInvoice,
        pi.SubTotalAmount,
        pi.TaxAmount,
        pi.TotalAmount,
        pi.StatusInvoice,
        pi.DescriptionInvoice,
        pi.ExchangeRateValue,
        pi.CreatedAt,
        pi.PurchaseInvoiceEntries.FirstOrDefault()?.IdAccountingEntry,
        pi.PurchaseInvoiceLines
            .Select(l => new PurchaseInvoiceLineResponse(
                l.IdPurchaseInvoiceLine,
                l.IdPurchaseInvoice,
                l.IdProductSKU,
                l.IdProductSKUNavigation?.CodeProductSKU,
                l.DescriptionLine,
                l.Quantity,
                l.UnitPrice,
                l.TaxPercent,
                l.TotalLineAmount))
            .ToList());

    // ── Consultas ─────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<PurchaseInvoiceResponse>> GetAllAsync(CancellationToken ct = default) =>
        (await BuildQuery()
            .OrderByDescending(pi => pi.DateInvoice)
            .ThenByDescending(pi => pi.IdPurchaseInvoice)
            .ToListAsync(ct))
            .Select(ToResponse).ToList();

    public async Task<IReadOnlyList<PurchaseInvoiceResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default) =>
        (await BuildQuery()
            .Where(pi => pi.IdFiscalPeriod == idFiscalPeriod)
            .OrderByDescending(pi => pi.DateInvoice)
            .ThenByDescending(pi => pi.IdPurchaseInvoice)
            .ToListAsync(ct))
            .Select(ToResponse).ToList();

    public async Task<PurchaseInvoiceResponse?> GetByIdAsync(int idPurchaseInvoice, CancellationToken ct = default)
    {
        var entity = await BuildQuery()
            .FirstOrDefaultAsync(pi => pi.IdPurchaseInvoice == idPurchaseInvoice, ct);
        return entity is null ? null : ToResponse(entity);
    }

    // ── CREAR ─────────────────────────────────────────────────────────────────
    public async Task<PurchaseInvoiceResponse> CreateAsync(CreatePurchaseInvoiceRequest request, CancellationToken ct = default)
    {
        var entity = new PurchaseInvoice
        {
            IdFiscalPeriod        = request.IdFiscalPeriod,
            IdCurrency            = request.IdCurrency,
            IdPurchaseInvoiceType = request.IdPurchaseInvoiceType,
            IdBankAccount         = request.IdBankAccount,
            NumberInvoice         = request.NumberInvoice.Trim(),
            ProviderName          = request.ProviderName.Trim(),
            DateInvoice           = request.DateInvoice,
            SubTotalAmount        = request.SubTotalAmount,
            TaxAmount             = request.TaxAmount,
            TotalAmount           = request.TotalAmount,
            StatusInvoice         = "Borrador",
            DescriptionInvoice    = string.IsNullOrWhiteSpace(request.DescriptionInvoice) ? null : request.DescriptionInvoice.Trim(),
            ExchangeRateValue     = request.ExchangeRateValue,
            CreatedAt             = DateTime.UtcNow,
        };

        var skuCodeToId = await UpsertSkusAsync(request.Lines, ct);
        entity.PurchaseInvoiceLines = request.Lines.Select(l => MapLine(l, skuCodeToId)).ToList();

        db.PurchaseInvoice.Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);

        return (await GetByIdAsync(entity.IdPurchaseInvoice, ct))!;
    }

    // ── ACTUALIZAR ────────────────────────────────────────────────────────────
    public async Task<PurchaseInvoiceResponse?> UpdateAsync(int idPurchaseInvoice, UpdatePurchaseInvoiceRequest request, CancellationToken ct = default)
    {
        var entity = await db.PurchaseInvoice
            .Include(pi => pi.PurchaseInvoiceLines)
            .FirstOrDefaultAsync(pi => pi.IdPurchaseInvoice == idPurchaseInvoice, ct);

        if (entity is null) return null;

        if (entity.StatusInvoice != "Borrador")
            throw new InvalidOperationException($"Solo se pueden modificar facturas en estado 'Borrador'. Estado actual: '{entity.StatusInvoice}'.");

        entity.IdFiscalPeriod        = request.IdFiscalPeriod;
        entity.IdCurrency            = request.IdCurrency;
        entity.IdPurchaseInvoiceType = request.IdPurchaseInvoiceType;
        entity.IdBankAccount         = request.IdBankAccount;
        entity.NumberInvoice         = request.NumberInvoice.Trim();
        entity.ProviderName          = request.ProviderName.Trim();
        entity.DateInvoice           = request.DateInvoice;
        entity.SubTotalAmount        = request.SubTotalAmount;
        entity.TaxAmount             = request.TaxAmount;
        entity.TotalAmount           = request.TotalAmount;
        entity.DescriptionInvoice    = string.IsNullOrWhiteSpace(request.DescriptionInvoice) ? null : request.DescriptionInvoice.Trim();
        entity.ExchangeRateValue     = request.ExchangeRateValue;

        db.PurchaseInvoiceLine.RemoveRange(entity.PurchaseInvoiceLines);
        entity.PurchaseInvoiceLines.Clear();

        var skuCodeToId = await UpsertSkusAsync(request.Lines, ct);
        foreach (var l in request.Lines)
            entity.PurchaseInvoiceLines.Add(MapLine(l, skuCodeToId));

        await db.SaveChangesAsync(CancellationToken.None);

        return await GetByIdAsync(idPurchaseInvoice, ct);
    }

    // ── ELIMINAR ──────────────────────────────────────────────────────────────
    public async Task<bool> DeleteAsync(int idPurchaseInvoice, CancellationToken ct = default)
    {
        var entity = await db.PurchaseInvoice
            .AsNoTracking()
            .FirstOrDefaultAsync(pi => pi.IdPurchaseInvoice == idPurchaseInvoice, ct);

        if (entity is null) return false;
        if (entity.StatusInvoice != "Borrador")
            throw new InvalidOperationException("Solo se pueden eliminar facturas en estado 'Borrador'.");

        var deleted = await db.PurchaseInvoice
            .Where(pi => pi.IdPurchaseInvoice == idPurchaseInvoice)
            .ExecuteDeleteAsync(CancellationToken.None);

        return deleted > 0;
    }

    // ── CONFIRMAR ─────────────────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, PurchaseInvoiceResponse? Invoice)> ConfirmAsync(
        int idPurchaseInvoice, CancellationToken ct = default)
    {
        // Cargar factura completa con todo lo necesario para generar el asiento
        var invoice = await db.PurchaseInvoice
            .Include(pi => pi.IdPurchaseInvoiceTypeNavigation)
                .ThenInclude(pit => pit.IdBankMovementTypeNavigation)
            .Include(pi => pi.IdCurrencyNavigation)
            .Include(pi => pi.PurchaseInvoiceLines)
            .Include(pi => pi.BankMovementDocuments)
                .ThenInclude(doc => doc.IdBankMovementNavigation)
                    .ThenInclude(bm => bm.IdBankAccountNavigation)
            .FirstOrDefaultAsync(pi => pi.IdPurchaseInvoice == idPurchaseInvoice, ct);

        if (invoice is null)
            return (false, "Factura no encontrada.", null);

        if (invoice.StatusInvoice != "Borrador")
            return (false, $"Solo se pueden confirmar facturas en estado 'Borrador'. Estado actual: '{invoice.StatusInvoice}'.", null);

        var invoiceType = invoice.IdPurchaseInvoiceTypeNavigation;

        // Determinar cuenta CR del asiento
        int? crAccountId;
        if (!invoiceType.CounterpartFromBankMovement)
        {
            // EFECTIVO: cuenta Caja fija según moneda
            bool isUsd = invoice.IdCurrencyNavigation.CodeCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase);
            crAccountId = isUsd ? invoiceType.IdAccountCounterpartUSD : invoiceType.IdAccountCounterpartCRC;

            if (crAccountId is null)
                return (false,
                    $"El tipo de factura '{invoiceType.CodePurchaseInvoiceType}' no tiene configurada la cuenta Caja para la moneda '{invoice.IdCurrencyNavigation.CodeCurrency}'. Configure IdAccountCounterpartCRC o IdAccountCounterpartUSD en el tipo de factura.",
                    null);
        }
        else
        {
            // DEBITO / TC: CR desde BankAccount del movimiento vinculado
            var bankDoc = invoice.BankMovementDocuments.FirstOrDefault();
            if (bankDoc is null)
            {
                // Auto-crear BankMovement + BankMovementDocument
                if (invoice.IdBankAccount is null)
                    return (false,
                        $"La factura de tipo '{invoiceType.CodePurchaseInvoiceType}' requiere una cuenta bancaria (IdBankAccount) para crear automáticamente el movimiento bancario.",
                        null);

                if (invoiceType.IdBankMovementType is null || invoiceType.IdBankMovementTypeNavigation is null)
                    return (false,
                        $"El tipo de factura '{invoiceType.CodePurchaseInvoiceType}' no tiene configurado el tipo de movimiento bancario (IdBankMovementType). Configure el catálogo.",
                        null);

                var bankAccount = await db.BankAccount
                    .Include(ba => ba.IdCurrencyNavigation)
                    .FirstOrDefaultAsync(ba => ba.IdBankAccount == invoice.IdBankAccount, ct);

                if (bankAccount is null)
                    return (false, "La cuenta bancaria vinculada a la factura no existe.", null);

                var movType  = invoiceType.IdBankMovementTypeNavigation;
                var isCargo  = movType.MovementSign == "Cargo";

                // Crear asiento del movimiento bancario
                var bkEntry = new AccountingEntry
                {
                    IdFiscalPeriod    = invoice.IdFiscalPeriod,
                    IdCurrency        = bankAccount.IdCurrency,
                    NumberEntry       = $"BK-FC-{invoice.IdPurchaseInvoice:D6}",
                    DateEntry         = invoice.DateInvoice,
                    DescriptionEntry  = $"Movimiento bancario — Factura {invoice.NumberInvoice} ({invoice.ProviderName})",
                    StatusEntry       = "Publicado",
                    ReferenceEntry    = invoice.NumberInvoice,
                    ExchangeRateValue = invoice.ExchangeRateValue,
                    OriginModule      = "BankMovement",
                    CreatedAt         = DateTime.UtcNow,
                    AccountingEntryLines =
                    [
                        new AccountingEntryLine
                        {
                            IdAccount       = isCargo ? bankAccount.IdAccount : movType.IdAccountCounterpart,
                            DebitAmount     = invoice.TotalAmount,
                            CreditAmount    = 0,
                            DescriptionLine = $"Factura {invoice.NumberInvoice} — {invoice.ProviderName}"
                        },
                        new AccountingEntryLine
                        {
                            IdAccount       = isCargo ? movType.IdAccountCounterpart : bankAccount.IdAccount,
                            DebitAmount     = 0,
                            CreditAmount    = invoice.TotalAmount,
                            DescriptionLine = $"Factura {invoice.NumberInvoice} — {invoice.ProviderName}"
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
                    NumberMovement      = $"FC-{invoice.IdPurchaseInvoice:D6}",
                    DateMovement        = invoice.DateInvoice,
                    DescriptionMovement = $"Factura {invoice.NumberInvoice} — {invoice.ProviderName}",
                    Amount              = invoice.TotalAmount,
                    StatusMovement      = "Confirmado",
                    ReferenceMovement   = invoice.NumberInvoice,
                    ExchangeRateValue   = invoice.ExchangeRateValue,
                    CreatedAt           = DateTime.UtcNow,
                    IdAccountingEntry   = bkEntry.IdAccountingEntry
                };

                db.BankMovement.Add(newMovement);
                await db.SaveChangesAsync(CancellationToken.None);

                var newDoc = new BankMovementDocument
                {
                    IdBankMovement     = newMovement.IdBankMovement,
                    IdPurchaseInvoice  = invoice.IdPurchaseInvoice,
                    TypeDocument       = "FacturaCompra",
                    NumberDocument     = invoice.NumberInvoice,
                    DateDocument       = invoice.DateInvoice,
                    AmountDocument     = invoice.TotalAmount,
                    DescriptionDocument = invoice.ProviderName
                };

                db.BankMovementDocument.Add(newDoc);
                await db.SaveChangesAsync(CancellationToken.None);

                crAccountId = bankAccount.IdAccount;
            }
            else
            {
                crAccountId = bankDoc.IdBankMovementNavigation.IdBankAccountNavigation.IdAccount;
            }
        }

        // Generar líneas DR desde ProductAccount para líneas con SKU
        var drLines = new List<(AccountingEntryLine Line, PurchaseInvoiceLine SourceLine)>();

        foreach (var invoiceLine in invoice.PurchaseInvoiceLines)
        {
            if (invoiceLine.IdProductSKU is not null)
            {
                var productAccounts = await db.ProductAccount
                    .AsNoTracking()
                    .Where(pa => db.ProductProductSKU
                        .Where(pps => pps.IdProductSKU == invoiceLine.IdProductSKU)
                        .Select(pps => pps.IdProduct)
                        .Contains(pa.IdProduct))
                    .ToListAsync(ct);

                if (productAccounts.Count > 0)
                {
                    foreach (var pa in productAccounts)
                    {
                        var rawAmount = Math.Round(invoiceLine.TotalLineAmount * pa.PercentageAccount / 100m, 2);
                        var absAmount = Math.Abs(rawAmount);
                        // Porcentaje negativo = contrapartida interna (CR); positivo = gasto (DR)
                        drLines.Add((new AccountingEntryLine
                        {
                            IdAccount       = pa.IdAccount,
                            DebitAmount     = rawAmount >= 0 ? absAmount : 0,
                            CreditAmount    = rawAmount <  0 ? absAmount : 0,
                            DescriptionLine = invoiceLine.DescriptionLine,
                            IdCostCenter    = pa.IdCostCenter
                        }, invoiceLine));
                    }
                    continue;
                }
            }

            // Fallback: sin ProductAccount → usar IdDefaultExpenseAccount del tipo de factura
            if (invoiceType.IdDefaultExpenseAccount is not null)
            {
                drLines.Add((new AccountingEntryLine
                {
                    IdAccount       = invoiceType.IdDefaultExpenseAccount.Value,
                    DebitAmount     = Math.Round(invoiceLine.TotalLineAmount, 2),
                    CreditAmount    = 0,
                    DescriptionLine = invoiceLine.DescriptionLine,
                }, invoiceLine));
            }
        }

        // Si no hay líneas, ni ProductAccount ni cuenta de gasto fallback configurada
        if (drLines.Count == 0)
            return (false,
                "No se pudo generar el asiento contable. Configure una cuenta de gasto fallback en el tipo de factura (IdDefaultExpenseAccount), o asigne distribución contable (ProductAccount) a los productos de las líneas.",
                null);

        // Calcular neto: DR positivos menos CR negativos (porcentajes negativos = contrapartidas internas)
        var totalDr = drLines.Sum(x => x.Line.DebitAmount - x.Line.CreditAmount);

        if (totalDr <= 0)
            return (false,
                $"El total neto del asiento es {totalDr:N2}. Los porcentajes de distribución deben resultar en un débito neto positivo.",
                null);

        // Línea CR (contrapartida — banco/caja)
        var crLine = new AccountingEntryLine
        {
            IdAccount       = crAccountId.Value,
            DebitAmount     = 0,
            CreditAmount    = totalDr,
            DescriptionLine = $"Factura {invoice.NumberInvoice} — {invoice.ProviderName}"
        };

        // Crear el asiento contable (OriginModule = "PurchaseInvoice")
        var entry = new AccountingEntry
        {
            IdFiscalPeriod    = invoice.IdFiscalPeriod,
            IdCurrency        = invoice.IdCurrency,
            NumberEntry       = $"FC-{invoice.IdPurchaseInvoice:D6}",
            DateEntry         = invoice.DateInvoice,
            DescriptionEntry  = $"Factura de compra #{invoice.NumberInvoice} — {invoice.ProviderName}",
            StatusEntry       = "Publicado",
            ReferenceEntry    = invoice.NumberInvoice,
            ExchangeRateValue = invoice.ExchangeRateValue,
            OriginModule      = "PurchaseInvoice",
            IdOriginRecord    = invoice.IdPurchaseInvoice,
            CreatedAt         = DateTime.UtcNow
        };

        foreach (var (line, _) in drLines) entry.AccountingEntryLines.Add(line);
        entry.AccountingEntryLines.Add(crLine);

        db.AccountingEntry.Add(entry);
        await db.SaveChangesAsync(CancellationToken.None);

        // Vincular asiento a la factura (PurchaseInvoiceEntry)
        db.PurchaseInvoiceEntry.Add(new PurchaseInvoiceEntry
        {
            IdPurchaseInvoice = invoice.IdPurchaseInvoice,
            IdAccountingEntry = entry.IdAccountingEntry
        });

        // Vincular líneas contables a líneas de factura (PurchaseInvoiceLineEntry)
        foreach (var (line, sourceLine) in drLines)
        {
            db.PurchaseInvoiceLineEntry.Add(new PurchaseInvoiceLineEntry
            {
                IdPurchaseInvoiceLine = sourceLine.IdPurchaseInvoiceLine,
                IdAccountingEntryLine = line.IdAccountingEntryLine
            });
        }

        // Actualizar estado de la factura
        var invoiceToUpdate = await db.PurchaseInvoice.FindAsync([invoice.IdPurchaseInvoice], ct);
        invoiceToUpdate!.StatusInvoice = "Confirmado";

        await db.SaveChangesAsync(CancellationToken.None);

        return (true, null, await GetByIdAsync(invoice.IdPurchaseInvoice, ct));
    }

    // ── ANULAR ────────────────────────────────────────────────────────────────
    public async Task<PurchaseInvoiceResponse?> CancelAsync(int idPurchaseInvoice, CancellationToken ct = default)
    {
        var entity = await db.PurchaseInvoice.FindAsync([idPurchaseInvoice], ct);
        if (entity is null) return null;

        if (entity.StatusInvoice == "Anulado")
            return await GetByIdAsync(idPurchaseInvoice, ct);

        if (entity.StatusInvoice != "Confirmado" && entity.StatusInvoice != "Borrador")
            throw new InvalidOperationException($"No se puede anular una factura en estado '{entity.StatusInvoice}'.");

        entity.StatusInvoice = "Anulado";

        // Anular asientos contables vinculados a la factura
        var entryIds = await db.PurchaseInvoiceEntry
            .Where(pie => pie.IdPurchaseInvoice == idPurchaseInvoice)
            .Select(pie => pie.IdAccountingEntry)
            .ToListAsync(CancellationToken.None);

        if (entryIds.Count > 0)
            await db.AccountingEntry
                .Where(ae => entryIds.Contains(ae.IdAccountingEntry) && ae.StatusEntry != "Anulado")
                .ExecuteUpdateAsync(s => s.SetProperty(ae => ae.StatusEntry, "Anulado"), CancellationToken.None);

        await db.SaveChangesAsync(CancellationToken.None);

        return await GetByIdAsync(idPurchaseInvoice, ct);
    }

    // ── Helpers privados ──────────────────────────────────────────────────────
    private async Task<IReadOnlyDictionary<string, int>> UpsertSkusAsync(
        IEnumerable<PurchaseInvoiceLineRequest> lines, CancellationToken ct)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var l in lines.Where(l => !string.IsNullOrWhiteSpace(l.SkuCode)))
        {
            var code = l.SkuCode!.Trim();
            if (result.ContainsKey(code)) continue;

            var name = string.IsNullOrWhiteSpace(l.SkuName)
                ? l.DescriptionLine.Trim()
                : l.SkuName.Trim();

            var sku = await db.ProductSKU.FirstOrDefaultAsync(s => s.CodeProductSKU == code, ct);
            if (sku is null)
            {
                sku = new ProductSKU { CodeProductSKU = code, NameProductSKU = name };
                db.ProductSKU.Add(sku);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            else if (sku.NameProductSKU != name)
            {
                sku.NameProductSKU = name;
                await db.SaveChangesAsync(CancellationToken.None);
            }
            result[code] = sku.IdProductSKU;
        }
        return result;
    }

    private static PurchaseInvoiceLine MapLine(
        PurchaseInvoiceLineRequest l, IReadOnlyDictionary<string, int> skuCodeToId) => new()
    {
        IdProductSKU    = !string.IsNullOrWhiteSpace(l.SkuCode)
                          && skuCodeToId.TryGetValue(l.SkuCode.Trim(), out var id) ? id : null,
        DescriptionLine = l.DescriptionLine.Trim(),
        Quantity        = l.Quantity,
        UnitPrice       = l.UnitPrice,
        TaxPercent      = l.TaxPercent,
        TotalLineAmount = l.TotalLineAmount
    };
}
