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
                l.IsNonProductLine,
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
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await ConfirmInternalAsync(idSalesInvoice, ct);
            if (result.Success) await tx.CommitAsync(ct);
            else await tx.RollbackAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<(bool Success, string? Error, SalesInvoiceResponse? Invoice)> ConfirmInternalAsync(
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
        // SourceLine es nullable: la línea de IVA por Pagar no corresponde a ninguna línea de factura específica.
        var crLines = new List<(AccountingEntryLine Line, SalesInvoiceLine? SourceLine)>();
        bool isUsdSale = invoice.IdCurrencyNavigation.CodeCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase);
        int ivaPorPagarId = isUsdSale ? 128 : 127;

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
                    var totalPct = productAccounts.Sum(pa => pa.PercentageAccount);
                    if (totalPct != 100m)
                        return (false,
                            $"La distribución contable del producto {invoiceLine.IdProduct} suma {totalPct:N2}% en lugar de 100%. Corrija los porcentajes antes de confirmar.",
                            null);

                    // Usar monto neto (sin IVA) para la cuenta de ingresos
                    var lineNetAmount = Math.Round(invoiceLine.Quantity * invoiceLine.UnitPrice, 2);
                    foreach (var pa in productAccounts)
                    {
                        var rawAmount = Math.Round(lineNetAmount * pa.PercentageAccount / 100m, 2);
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
                var lineNetAmount = Math.Round(invoiceLine.Quantity * invoiceLine.UnitPrice, 2);
                crLines.Add((new AccountingEntryLine
                {
                    IdAccount       = invoiceType.IdAccountSalesRevenue.Value,
                    DebitAmount     = 0,
                    CreditAmount    = lineNetAmount,
                    DescriptionLine = invoiceLine.DescriptionLine,
                }, invoiceLine));
            }
        }

        // Línea IVA por Pagar: el IVA cobrado al cliente es un pasivo a declarar al gobierno
        if (invoice.TaxAmount > 0m)
        {
            crLines.Add((new AccountingEntryLine
            {
                IdAccount       = ivaPorPagarId,
                DebitAmount     = 0,
                CreditAmount    = invoice.TaxAmount,
                DescriptionLine = $"IVA por Pagar — Venta {invoice.IdSalesInvoice:D6}"
            }, null));
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
        // Se omite la línea de IVA por Pagar porque no corresponde a una línea de factura específica.
        foreach (var (line, sourceLine) in crLines)
        {
            if (sourceLine is null) continue;
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

        // ── 5. Decrementar inventario y registrar COGS (BOM 2B + Combo 3A) ─────────────────

        // Validación previa: líneas de producto sin receta activa ni combo deben llevar lote
        foreach (var invoiceLine in invoice.SalesInvoiceLines)
        {
            if (invoiceLine.IsNonProductLine || invoiceLine.IdProduct is null) continue;

            var prodType = await db.Product
                .AsNoTracking()
                .Include(p => p.IdProductTypeNavigation)
                .Where(p => p.IdProduct == invoiceLine.IdProduct.Value)
                .Select(p => new { p.IsCombo, p.IsVariantParent, p.NameProduct, p.IdProductTypeNavigation.TrackInventory })
                .FirstOrDefaultAsync(CancellationToken.None);

            if (prodType is null || !prodType.TrackInventory) continue;

            if (prodType.IsVariantParent)
                return (false,
                    $"El producto '{prodType.NameProduct}' es un producto padre con variantes. Debe seleccionar una variante específica en la línea.",
                    null);

            if (prodType.IsCombo) continue;

            var hasRecipe = await db.ProductRecipe.AnyAsync(
                r => r.IdProductOutput == invoiceLine.IdProduct.Value && r.IsActive,
                CancellationToken.None);
            if (hasRecipe) continue;

            if (invoiceLine.IdInventoryLot is null)
                return (false,
                    $"La línea '{invoiceLine.DescriptionLine}' es un producto con inventario, sin receta activa y sin combo. Debe asignar un lote (IdInventoryLot) o marcar como IsNonProductLine si no lleva stock.",
                    null);
        }

        var cogsLines  = new List<(AccountingEntryLine Line, SalesInvoiceLine SourceLine)>();
        var totalCogsAmount = 0m;

        foreach (var invoiceLine in invoice.SalesInvoiceLines)
        {
            if (invoiceLine.IsNonProductLine || invoiceLine.IdProduct is null) continue;

            var prod = await db.Product
                .Include(p => p.IdProductTypeNavigation)
                .FirstOrDefaultAsync(p => p.IdProduct == invoiceLine.IdProduct.Value, CancellationToken.None);

            if (prod is null || !prod.IdProductTypeNavigation.TrackInventory) continue;

            // Calcular qtyBase de la línea (cantidad en unidad base del producto vendido)
            var puLine = invoiceLine.IdUnit.HasValue
                ? await db.ProductUnit.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IdProduct == invoiceLine.IdProduct.Value && u.IdUnit == invoiceLine.IdUnit.Value, CancellationToken.None)
                : null;
            decimal lineQtyBase = Math.Round(invoiceLine.Quantity * (puLine?.ConversionFactor ?? 1m), 6);

            string? bomError;

            if (prod.IsCombo)
            {
                // ── 3A: Explosión de combo ────────────────────────────────────────────
                (totalCogsAmount, bomError) = await ExplodeComboAsync(
                    invoice, invoiceLine, lineQtyBase, invoiceType, cogsLines, totalCogsAmount, CancellationToken.None);

                if (bomError is not null)
                    return (false, bomError, null);
            }
            else
            {
                var recipe = await db.ProductRecipe
                    .Include(r => r.ProductRecipeLines)
                    .FirstOrDefaultAsync(r => r.IdProductOutput == invoiceLine.IdProduct.Value && r.IsActive, CancellationToken.None);

                // T19: Si la línea ya tiene un lote PT (vino de producción), saltar
                // la explosión BOM y descontar el lote directamente.
                bool hasPreassignedLot = invoiceLine.IdInventoryLot.HasValue && recipe is not null;

                if (recipe is not null && !hasPreassignedLot)
                {
                    // ── 2B: Explosión BOM (receta activa) ─────────────────────────────
                    (totalCogsAmount, bomError) = await ExplodeBomAsync(
                        invoice, invoiceLine, lineQtyBase, recipe, null, invoiceType, cogsLines, totalCogsAmount, CancellationToken.None);

                    if (bomError is not null)
                        return (false, bomError, null);

                    // Snapshot de receta usada en la línea
                    var trackedBomLine = await db.SalesInvoiceLine.FindAsync([invoiceLine.IdSalesInvoiceLine], CancellationToken.None);
                    if (trackedBomLine is not null)
                    {
                        trackedBomLine.IdProductRecipe = recipe.IdProductRecipe;
                        trackedBomLine.QuantityBase    = lineQtyBase;
                    }
                    await db.SaveChangesAsync(CancellationToken.None);
                }
                else
                {
                    // ── Lote directo (comportamiento original) ────────────────────────
                    var lot = await db.InventoryLot.FindAsync([invoiceLine.IdInventoryLot!.Value], CancellationToken.None);
                    if (lot is null) continue;

                    decimal lineCogs;
                    (lineCogs, bomError) = await DeductLotAsync(
                        lot, lineQtyBase, invoiceLine.IdProduct.Value, invoiceLine.DescriptionLine,
                        invoiceType, invoiceLine, cogsLines, invoice.DateInvoice, CancellationToken.None);

                    if (bomError is not null)
                        return (false, bomError, null);

                    totalCogsAmount += lineCogs;

                    var trackedLine = await db.SalesInvoiceLine.FindAsync([invoiceLine.IdSalesInvoiceLine], CancellationToken.None);
                    if (trackedLine is not null)
                    {
                        trackedLine.QuantityBase = lineQtyBase;
                        trackedLine.UnitCost     = lot.UnitCost;
                    }
                    await db.SaveChangesAsync(CancellationToken.None);
                }
            }
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
            // Revertir BomDetails (descarga BOM o combo)
            var bomDetails = await db.SalesInvoiceLineBomDetail
                .Where(d => d.IdSalesInvoiceLine == line.IdSalesInvoiceLine)
                .ToListAsync(CancellationToken.None);

            foreach (var detail in bomDetails)
            {
                var bomLot = await db.InventoryLot.FindAsync([detail.IdInventoryLot], CancellationToken.None);
                if (bomLot is null) continue;

                bomLot.QuantityAvailable += detail.QuantityConsumed;
                await db.SaveChangesAsync(CancellationToken.None);

                await RecalcAverageCostAsync(detail.IdProduct, CancellationToken.None);
            }

            // Revertir lote directo (líneas sin BOM)
            if (line.IdInventoryLot is null || line.QuantityBase is null) continue;

            var lot = await db.InventoryLot.FindAsync([line.IdInventoryLot.Value], CancellationToken.None);
            if (lot is null) continue;

            lot.QuantityAvailable += line.QuantityBase.Value;
            await db.SaveChangesAsync(CancellationToken.None);

            if (line.IdProduct is not null)
                await RecalcAverageCostAsync(line.IdProduct.Value, CancellationToken.None);
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

    // ── Helpers privados de inventario ───────────────────────────────────────

    /// Decrementa un lote, recalcula AverageCost y agrega la línea COGS.
    /// Retorna (cogsAmount, errorMessage). errorMessage == null => éxito.
    private async Task<(decimal CogsAmount, string? Error)> DeductLotAsync(
        InventoryLot lot, decimal qtyBase, int idProduct, string description,
        SalesInvoiceType invoiceType, SalesInvoiceLine sourceLine,
        List<(AccountingEntryLine, SalesInvoiceLine)> cogsLines,
        DateOnly referenceDate,
        CancellationToken ct)
    {
        if (lot.ExpirationDate.HasValue && lot.ExpirationDate.Value < referenceDate)
            return (0m, $"El lote '{lot.LotNumber ?? lot.IdInventoryLot.ToString()}' del producto {idProduct} está vencido.");

        if (invoiceType.IdAccountCOGS is null)
            return (0m, $"El tipo de factura de venta no tiene configurada la cuenta COGS (IdAccountCOGS). Configure el catálogo antes de confirmar.");

        decimal unitCost       = lot.UnitCost;
        decimal lineCogsAmount = Math.Round(qtyBase * unitCost, 2);

        lot.QuantityAvailable -= qtyBase;
        if (lot.QuantityAvailable < 0)
            return (0m, $"El lote '{lot.LotNumber ?? lot.IdInventoryLot.ToString()}' del producto {idProduct} quedaría con stock negativo ({lot.QuantityAvailable + qtyBase:N4} disponible, se intenta decrementar {qtyBase:N4}).");

        // Liberar la reserva que se creó al asignar el SalesOrderLineFulfillment (M3)
        lot.QuantityReserved = Math.Max(0m, lot.QuantityReserved - qtyBase);

        await db.SaveChangesAsync(ct);
        await RecalcAverageCostAsync(idProduct, ct);

        cogsLines.Add((new AccountingEntryLine
        {
            IdAccount       = invoiceType.IdAccountCOGS.Value,
            DebitAmount     = lineCogsAmount,
            CreditAmount    = 0,
            DescriptionLine = description
        }, sourceLine));

        return (lineCogsAmount, null);
    }

    /// Recalcula Product.AverageCost a partir de los lotes disponibles.
    private async Task RecalcAverageCostAsync(int idProduct, CancellationToken ct)
    {
        var product = await db.Product.FindAsync([idProduct], ct);
        var allLots = await db.InventoryLot
            .Where(il => il.IdProduct == idProduct)
            .ToListAsync(ct);

        var totalQty  = allLots.Sum(il => il.QuantityAvailable);
        var totalCost = allLots.Sum(il => il.QuantityAvailable * il.UnitCost);
        if (product is not null)
            product.AverageCost = totalQty > 0 ? Math.Round(totalCost / totalQty, 6) : 0m;
    }

    /// Obtiene el primer lote FEFO con stock neto disponible (QuantityAvailable - QuantityReserved) y no vencido para el producto.
    private Task<InventoryLot?> GetFefoLotAsync(int idProduct, DateOnly referenceDate)
        => db.InventoryLot
            .Where(il => il.IdProduct == idProduct
                      && il.StatusLot == "Disponible"               // solo lotes disponibles
                      && il.QuantityAvailable > il.QuantityReserved  // stock neto > 0
                      && (il.ExpirationDate == null || il.ExpirationDate >= referenceDate))
            .OrderBy(il => il.ExpirationDate == null ? 1 : 0)
            .ThenBy(il => il.ExpirationDate)
            .ThenBy(il => il.IdInventoryLot)
            .FirstOrDefaultAsync();

    /// 2B — Explota una receta para una línea de factura.
    /// Crea un SalesInvoiceLineBomDetail por cada insumo y descuenta FEFO.
    /// comboSlotId es FK nullable cuando se llama desde ExplodeComboAsync.
    private async Task<(decimal TotalCogs, string? Error)> ExplodeBomAsync(
        SalesInvoice invoice, SalesInvoiceLine sourceLine, decimal lineQtyBase,
        ProductRecipe recipe, int? comboSlotId,
        SalesInvoiceType invoiceType,
        List<(AccountingEntryLine, SalesInvoiceLine)> cogsLines,
        decimal runningTotal, CancellationToken ct)
    {
        foreach (var recipeLine in recipe.ProductRecipeLines)
        {
            decimal qtyToConsume = Math.Round(recipeLine.QuantityInput * lineQtyBase, 6);

            var lot = await GetFefoLotAsync(recipeLine.IdProductInput, invoice.DateInvoice);
            if (lot is null)
                return (runningTotal, $"Sin stock disponible para el insumo '{recipeLine.IdProductInput}' requerido por la receta '{recipe.NameRecipe}'. Verifique lotes o realice un ajuste de inventario.");

            var (lineCogs, error) = await DeductLotAsync(
                lot, qtyToConsume, recipeLine.IdProductInput,
                $"BOM — {recipe.NameRecipe} / {sourceLine.DescriptionLine}",
                invoiceType, sourceLine, cogsLines, invoice.DateInvoice, ct);

            if (error is not null) return (runningTotal, error);

            db.SalesInvoiceLineBomDetail.Add(new SalesInvoiceLineBomDetail
            {
                IdSalesInvoiceLine  = sourceLine.IdSalesInvoiceLine,
                IdProductComboSlot  = comboSlotId,
                IdProductRecipeLine = recipeLine.IdProductRecipeLine,
                IdProduct           = recipeLine.IdProductInput,
                IdInventoryLot      = lot.IdInventoryLot,
                QuantityConsumed    = qtyToConsume,
                UnitCost            = lot.UnitCost
            });

            runningTotal += lineCogs;
        }

        await db.SaveChangesAsync(ct);
        return (runningTotal, null);
    }

    /// 3A — Explota un combo: itera slots, por cada producto:
    ///   - Si tiene receta activa  → ExplodeBomAsync
    ///   - Si es reventa/directo   → FEFO + DeductLotAsync + BomDetail
    private async Task<(decimal TotalCogs, string? Error)> ExplodeComboAsync(
        SalesInvoice invoice, SalesInvoiceLine sourceLine, decimal lineQtyBase,
        SalesInvoiceType invoiceType,
        List<(AccountingEntryLine, SalesInvoiceLine)> cogsLines,
        decimal runningTotal, CancellationToken ct)
    {
        var slots = await db.ProductComboSlot
            .Include(s => s.ProductComboSlotProducts)
            .Where(s => s.IdProductCombo == sourceLine.IdProduct!.Value)
            .ToListAsync(ct);

        foreach (var slot in slots)
        {
            // Cantidad total del slot = cantidad del slot en el combo × cantidad en línea de factura
            decimal slotQty = Math.Round(slot.Quantity * lineQtyBase, 6);

            // Tomar el primer producto del slot (el operador lo puede variar desde la UI en versiones futuras)
            var slotProduct = slot.ProductComboSlotProducts.FirstOrDefault();
            if (slotProduct is null) continue;

            var recipe = await db.ProductRecipe
                .Include(r => r.ProductRecipeLines)
                .FirstOrDefaultAsync(r => r.IdProductOutput == slotProduct.IdProduct && r.IsActive, ct);

            if (recipe is not null)
            {
                // Slot con receta → explosión BOM
                (runningTotal, var bomError) = await ExplodeBomAsync(
                    invoice, sourceLine, slotQty, recipe, slot.IdProductComboSlot,
                    invoiceType, cogsLines, runningTotal, ct);

                if (bomError is not null) return (runningTotal, bomError);
            }
            else
            {
                // Slot sin receta (reventa / producto terminado con lote propio) → FEFO directo
                var prodType = await db.Product
                    .AsNoTracking()
                    .Include(p => p.IdProductTypeNavigation)
                    .Where(p => p.IdProduct == slotProduct.IdProduct)
                    .Select(p => p.IdProductTypeNavigation.TrackInventory)
                    .FirstOrDefaultAsync(ct);

                if (!prodType) continue;   // servicio / no inventariable → omitir

                var lot = await GetFefoLotAsync(slotProduct.IdProduct, invoice.DateInvoice);
                if (lot is null)
                    return (runningTotal, $"Sin stock disponible para el producto '{slotProduct.IdProduct}' del slot '{slot.NameSlot}' en el combo '{sourceLine.DescriptionLine}'.");

                var (lineCogs, slotError) = await DeductLotAsync(
                    lot, slotQty, slotProduct.IdProduct,
                    $"Combo slot '{slot.NameSlot}' — {sourceLine.DescriptionLine}",
                    invoiceType, sourceLine, cogsLines, invoice.DateInvoice, ct);

                if (slotError is not null) return (runningTotal, slotError);

                db.SalesInvoiceLineBomDetail.Add(new SalesInvoiceLineBomDetail
                {
                    IdSalesInvoiceLine  = sourceLine.IdSalesInvoiceLine,
                    IdProductComboSlot  = slot.IdProductComboSlot,
                    IdProductRecipeLine = null,   // reventa directa — no hay línea de receta
                    IdProduct           = slotProduct.IdProduct,
                    IdInventoryLot      = lot.IdInventoryLot,
                    QuantityConsumed    = slotQty,
                    UnitCost            = lot.UnitCost
                });

                await db.SaveChangesAsync(ct);
                runningTotal += lineCogs;
            }
        }

        return (runningTotal, null);
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
                IsNonProductLine = l.IsNonProductLine,
                IdProduct        = l.IdProduct,
                IdUnit           = l.IdUnit,
                IdInventoryLot   = l.IdInventoryLot,
                DescriptionLine  = l.DescriptionLine.Trim(),
                Quantity         = l.Quantity,
                QuantityBase     = qtyBase,
                UnitPrice        = l.UnitPrice,
                TaxPercent       = l.TaxPercent,
                TotalLineAmount  = l.TotalLineAmount
            });
        }

        return (result, null);
    }

    // ── DEVOLUCIÓN PARCIAL ────────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, PartialReturnResponse? Result)> PartialReturnAsync(
        int idSalesInvoice, PartialReturnRequest request, CancellationToken ct = default)
    {
        // ── Validar RefundMode antes de tocar la BD ────────────────────────────
        if (request.RefundMode is not ("EfectivoInmediato" or "NotaCredito"))
            return (false, "RefundMode inválido. Use 'EfectivoInmediato' o 'NotaCredito'.", null);

        if (request.RefundMode == "NotaCredito" && request.IdAccountCreditNote is null)
            return (false, "Para RefundMode='NotaCredito' se debe indicar IdAccountCreditNote.", null);

        var invoice = await db.SalesInvoice
            .Include(si => si.IdSalesInvoiceTypeNavigation)
            .Include(si => si.IdCurrencyNavigation)
            .FirstOrDefaultAsync(si => si.IdSalesInvoice == idSalesInvoice, ct);

        if (invoice is null)
            return (false, "Factura de venta no encontrada.", null);

        if (invoice.StatusInvoice != "Confirmado")
            return (false,
                $"Solo se pueden registrar devoluciones en facturas 'Confirmado'. Estado actual: '{invoice.StatusInvoice}'.",
                null);

        var invoiceType = invoice.IdSalesInvoiceTypeNavigation;

        // ── 0. Validar que la cantidad devuelta no supere lo vendido ─────────
        var salesLines = await db.SalesInvoiceLine
            .Where(l => l.IdSalesInvoice == idSalesInvoice)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var line in request.Lines)
        {
            var salesLine = salesLines.FirstOrDefault(l => l.IdInventoryLot == line.IdInventoryLot);
            if (salesLine is null)
                return (false,
                    $"El lote {line.IdInventoryLot} no corresponde a ninguna línea de esta factura de venta.",
                    null);

            if (line.Quantity > salesLine.QuantityBase)
                return (false,
                    $"La cantidad a devolver ({line.Quantity:N4}) supera la cantidad originalmente vendida " +
                    $"({salesLine.QuantityBase:N4}) para el lote {line.IdInventoryLot}.",
                    null);
        }

        decimal totalReturnAmount = 0m;
        decimal totalCogsReversed = 0m;
        var cogsEntryLines = new List<AccountingEntryLine>();

        // ── 1. Restaurar inventario por lote ─────────────────────────────────
        foreach (var line in request.Lines)
        {
            var lot = await db.InventoryLot.FindAsync([line.IdInventoryLot], CancellationToken.None);
            if (lot is null)
                return (false, $"Lote de inventario {line.IdInventoryLot} no encontrado.", null);

            lot.QuantityAvailable += line.Quantity;
            await db.SaveChangesAsync(CancellationToken.None);
            await RecalcAverageCostAsync(lot.IdProduct, CancellationToken.None);

            decimal lineCogs = Math.Round(line.Quantity * lot.UnitCost, 2);
            totalCogsReversed += lineCogs;
            totalReturnAmount += line.TotalLineAmount;

            if (invoiceType.IdAccountCOGS is not null && invoiceType.IdAccountInventory is not null)
            {
                cogsEntryLines.Add(new AccountingEntryLine
                {
                    IdAccount       = invoiceType.IdAccountInventory.Value,
                    DebitAmount     = lineCogs,
                    CreditAmount    = 0,
                    DescriptionLine = line.DescriptionLine
                                      ?? $"Devolución — lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()}"
                });
            }
        }

        // ── 2. Asiento de reversión COGS (DR Inventario / CR COGS) ───────────
        int? idAccountingEntry = null;

        if (cogsEntryLines.Count > 0
            && invoiceType.IdAccountCOGS is not null
            && invoiceType.IdAccountInventory is not null)
        {
            // CR COGS: reduce el gasto de costo de ventas
            cogsEntryLines.Add(new AccountingEntryLine
            {
                IdAccount       = invoiceType.IdAccountCOGS.Value,
                DebitAmount     = 0,
                CreditAmount    = totalCogsReversed,
                DescriptionLine = $"Reversión COGS — devolución parcial FV-{idSalesInvoice:D6}"
            });

            var entry = new AccountingEntry
            {
                IdFiscalPeriod    = invoice.IdFiscalPeriod,
                IdCurrency        = invoice.IdCurrency,
                NumberEntry       = $"DEV-COGS-FV-{idSalesInvoice:D6}",
                DateEntry         = request.DateReturn,
                DescriptionEntry  = request.DescriptionReturn
                                    ?? $"Devolución parcial — {invoice.NumberInvoice}",
                StatusEntry       = "Publicado",
                ExchangeRateValue = invoice.ExchangeRateValue,
                OriginModule      = "SalesReturnPartial",
                IdOriginRecord    = idSalesInvoice,
                CreatedAt         = DateTime.UtcNow
            };

            foreach (var line in cogsEntryLines)
                entry.AccountingEntryLines.Add(line);

            db.AccountingEntry.Add(entry);
            await db.SaveChangesAsync(CancellationToken.None);

            db.SalesInvoiceEntry.Add(new SalesInvoiceEntry
            {
                IdSalesInvoice    = idSalesInvoice,
                IdAccountingEntry = entry.IdAccountingEntry
            });
            await db.SaveChangesAsync(CancellationToken.None);

            idAccountingEntry = entry.IdAccountingEntry;
        }

        // ── 3. Asiento de reversión de ingresos (DR Ingresos+IVA / CR Banco-Caja o NotaCredito) ──
        int? idAccountingEntryRefund = null;

        // Determinar cuenta CR de la contrapartida del reintegro
        int crRefundAccountId;
        if (request.RefundMode == "NotaCredito")
        {
            crRefundAccountId = request.IdAccountCreditNote!.Value;
        }
        else // EfectivoInmediato
        {
            var invoiceType2 = invoice.IdSalesInvoiceTypeNavigation;
            if (!invoiceType2.CounterpartFromBankMovement)
            {
                bool isUsdCurr = invoice.IdCurrencyNavigation.CodeCurrency
                    .Equals("USD", StringComparison.OrdinalIgnoreCase);
                crRefundAccountId = (isUsdCurr
                    ? invoiceType2.IdAccountCounterpartUSD
                    : invoiceType2.IdAccountCounterpartCRC) ?? 0;

                if (crRefundAccountId == 0)
                    return (false,
                        $"El tipo de factura '{invoiceType2.CodeSalesInvoiceType}' no tiene configurada la cuenta Caja " +
                        $"para la moneda '{invoice.IdCurrencyNavigation.CodeCurrency}'. " +
                        "Configure IdAccountCounterpartCRC o IdAccountCounterpartUSD.",
                        null);
            }
            else
            {
                if (invoice.IdBankAccount is null)
                    return (false,
                        "La factura no tiene una cuenta bancaria vinculada (IdBankAccount). " +
                        "No se puede generar el reintegro automático.",
                        null);

                var bkAcc = await db.BankAccount
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.IdBankAccount == invoice.IdBankAccount.Value, CancellationToken.None);

                if (bkAcc is null)
                    return (false, "La cuenta bancaria vinculada a la factura no existe.", null);

                crRefundAccountId = bkAcc.IdAccount;
            }
        }

        // Construir líneas DR por línea devuelta (separando subtotal e IVA)
        bool isUsdInvoice = invoice.IdCurrencyNavigation.CodeCurrency
            .Equals("USD", StringComparison.OrdinalIgnoreCase);
        int ivaAccountId = isUsdInvoice ? 128 : 127;

        decimal totalRefundGross = 0m;
        var refundLines = new List<AccountingEntryLine>();

        foreach (var line in request.Lines)
        {
            var salesLine = salesLines.First(sl => sl.IdInventoryLot == line.IdInventoryLot);

            decimal taxFactor  = salesLine.TaxPercent > 0 ? (1m + salesLine.TaxPercent / 100m) : 1m;
            decimal lineSubtotal = Math.Round(line.TotalLineAmount / taxFactor, 2);
            decimal lineTax      = Math.Round(line.TotalLineAmount - lineSubtotal, 2);
            totalRefundGross += line.TotalLineAmount;

            // Determinar cuenta(s) de ingresos: ProductAccount o fallback
            bool addedViaProductAccount = false;
            if (salesLine.IdProduct.HasValue)
            {
                var productAccounts = await db.ProductAccount
                    .AsNoTracking()
                    .Where(pa => pa.IdProduct == salesLine.IdProduct.Value)
                    .ToListAsync(CancellationToken.None);

                if (productAccounts.Count > 0)
                {
                    foreach (var pa in productAccounts)
                    {
                        decimal drAmt = Math.Round(lineSubtotal * pa.PercentageAccount / 100m, 2);
                        refundLines.Add(new AccountingEntryLine
                        {
                            IdAccount       = pa.IdAccount,
                            DebitAmount     = drAmt,
                            CreditAmount    = 0,
                            DescriptionLine = line.DescriptionLine
                                             ?? $"Reversión ingreso — lote {line.IdInventoryLot}"
                        });
                    }
                    addedViaProductAccount = true;
                }
            }

            if (!addedViaProductAccount)
            {
                if (invoiceType.IdAccountSalesRevenue is null)
                    return (false,
                        "El tipo de factura no tiene cuenta de ingresos configurada (IdAccountSalesRevenue). " +
                        "Configure el campo o asigne distribución contable (ProductAccount) al producto.",
                        null);

                refundLines.Add(new AccountingEntryLine
                {
                    IdAccount       = invoiceType.IdAccountSalesRevenue.Value,
                    DebitAmount     = lineSubtotal,
                    CreditAmount    = 0,
                    DescriptionLine = line.DescriptionLine
                                     ?? $"Reversión ingreso — lote {line.IdInventoryLot}"
                });
            }

            // DR IVA por Pagar (reduce el pasivo hacia el gobierno)
            if (lineTax > 0)
            {
                refundLines.Add(new AccountingEntryLine
                {
                    IdAccount       = ivaAccountId,
                    DebitAmount     = lineTax,
                    CreditAmount    = 0,
                    DescriptionLine = $"Reversión IVA — devolución lote {line.IdInventoryLot}"
                });
            }
        }

        // CR contrapartida total bruto
        refundLines.Add(new AccountingEntryLine
        {
            IdAccount       = crRefundAccountId,
            DebitAmount     = 0,
            CreditAmount    = totalRefundGross,
            DescriptionLine = $"Reintegro — devolución parcial FV-{idSalesInvoice:D6}"
        });

        var refundEntry = new AccountingEntry
        {
            IdFiscalPeriod    = invoice.IdFiscalPeriod,
            IdCurrency        = invoice.IdCurrency,
            NumberEntry       = $"DEV-ING-FV-{idSalesInvoice:D6}",
            DateEntry         = request.DateReturn,
            DescriptionEntry  = request.DescriptionReturn
                                ?? $"Reintegro devolución parcial — {invoice.NumberInvoice}",
            StatusEntry       = "Publicado",
            ExchangeRateValue = invoice.ExchangeRateValue,
            OriginModule      = "SalesReturnPartial",
            IdOriginRecord    = idSalesInvoice,
            CreatedAt         = DateTime.UtcNow
        };
        foreach (var rl in refundLines) refundEntry.AccountingEntryLines.Add(rl);

        db.AccountingEntry.Add(refundEntry);
        await db.SaveChangesAsync(CancellationToken.None);

        db.SalesInvoiceEntry.Add(new SalesInvoiceEntry
        {
            IdSalesInvoice    = idSalesInvoice,
            IdAccountingEntry = refundEntry.IdAccountingEntry
        });
        await db.SaveChangesAsync(CancellationToken.None);

        idAccountingEntryRefund = refundEntry.IdAccountingEntry;

        return (true, null, new PartialReturnResponse(
            idSalesInvoice,
            invoice.NumberInvoice,
            request.DateReturn,
            request.DescriptionReturn,
            totalReturnAmount,
            idAccountingEntry,
            idAccountingEntryRefund));
    }
}
