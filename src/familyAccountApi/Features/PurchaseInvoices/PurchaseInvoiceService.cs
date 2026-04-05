using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Contacts;
using FamilyAccountApi.Features.PurchaseInvoices.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.PurchaseInvoices;

public sealed class PurchaseInvoiceService(AppDbContext db, IContactService contacts) : IPurchaseInvoiceService
{
    // ── Proyección compartida ─────────────────────────────────────────────────
    private IQueryable<PurchaseInvoice> BuildQuery() =>
        db.PurchaseInvoice
            .AsNoTracking()
            .Include(pi => pi.IdFiscalPeriodNavigation)
            .Include(pi => pi.IdCurrencyNavigation)
            .Include(pi => pi.IdPurchaseInvoiceTypeNavigation)
            .Include(pi => pi.IdBankAccountNavigation)
            .Include(pi => pi.IdContactNavigation)
            .Include(pi => pi.IdWarehouseNavigation)
            .Include(pi => pi.PurchaseInvoiceLines)
                .ThenInclude(l => l.IdProductNavigation)
            .Include(pi => pi.PurchaseInvoiceLines)
                .ThenInclude(l => l.IdUnitNavigation)
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
        pi.IdContact,
        pi.IdContactNavigation?.Name,
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
        pi.IdWarehouse,
        pi.IdWarehouseNavigation?.NameWarehouse,
        pi.PurchaseInvoiceLines
            .Select(l => new PurchaseInvoiceLineResponse(
                l.IdPurchaseInvoiceLine,
                l.IdPurchaseInvoice,
                l.IdProduct,
                l.IdProductNavigation?.NameProduct,
                l.IdUnit,
                l.IdUnitNavigation?.CodeUnit,
                l.DescriptionLine,
                l.Quantity,
                l.QuantityBase ?? l.Quantity,
                l.UnitPrice,
                l.TaxPercent,
                l.TotalLineAmount,
                l.LotNumber,
                l.ExpirationDate))
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
        var (idContact, providerName) = await ResolveContactAsync(request.IdContact, request.ProviderName, ct);

        var entity = new PurchaseInvoice
        {
            IdFiscalPeriod        = request.IdFiscalPeriod,
            IdCurrency            = request.IdCurrency,
            IdPurchaseInvoiceType = request.IdPurchaseInvoiceType,
            IdBankAccount         = request.IdBankAccount,
            IdContact             = idContact,
            IdWarehouse           = request.IdWarehouse,
            NumberInvoice         = request.NumberInvoice.Trim(),
            ProviderName          = providerName,
            DateInvoice           = request.DateInvoice,
            SubTotalAmount        = request.SubTotalAmount,
            TaxAmount             = request.TaxAmount,
            TotalAmount           = request.TotalAmount,
            StatusInvoice         = "Borrador",
            DescriptionInvoice    = string.IsNullOrWhiteSpace(request.DescriptionInvoice) ? null : request.DescriptionInvoice.Trim(),
            ExchangeRateValue     = request.ExchangeRateValue,
            CreatedAt             = DateTime.UtcNow,
        };

        var (mappedLines, linesError) = await MapLinesAsync(request.Lines, ct);
        if (linesError is not null)
            throw new InvalidOperationException(linesError);

        entity.PurchaseInvoiceLines = mappedLines;

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

        var (idContact, providerName) = await ResolveContactAsync(request.IdContact, request.ProviderName, ct);

        entity.IdFiscalPeriod        = request.IdFiscalPeriod;
        entity.IdCurrency            = request.IdCurrency;
        entity.IdPurchaseInvoiceType = request.IdPurchaseInvoiceType;
        entity.IdBankAccount         = request.IdBankAccount;
        entity.IdContact             = idContact;
        entity.IdWarehouse           = request.IdWarehouse;
        entity.NumberInvoice         = request.NumberInvoice.Trim();
        entity.ProviderName          = providerName;
        entity.DateInvoice           = request.DateInvoice;
        entity.SubTotalAmount        = request.SubTotalAmount;
        entity.TaxAmount             = request.TaxAmount;
        entity.TotalAmount           = request.TotalAmount;
        entity.DescriptionInvoice    = string.IsNullOrWhiteSpace(request.DescriptionInvoice) ? null : request.DescriptionInvoice.Trim();
        entity.ExchangeRateValue     = request.ExchangeRateValue;

        var (mappedLines, linesError) = await MapLinesAsync(request.Lines, ct);
        if (linesError is not null)
            throw new InvalidOperationException(linesError);

        db.PurchaseInvoiceLine.RemoveRange(entity.PurchaseInvoiceLines);
        entity.PurchaseInvoiceLines.Clear();

        foreach (var line in mappedLines)
            entity.PurchaseInvoiceLines.Add(line);

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

        // ── L1: Crear lotes de inventario y actualizar QuantityBase ─────────
        var warehouseId = invoice.IdWarehouse
            ?? await db.Warehouse
                .Where(w => w.IsDefault)
                .Select(w => w.IdWarehouse)
                .FirstOrDefaultAsync(CancellationToken.None);

        if (warehouseId == 0)
            return (false,
                "La factura no tiene almacén asignado y no existe un almacén predeterminado. " +
                "Asigne un almacén a la factura o configure uno como predeterminado.",
                null);

        foreach (var line in invoice.PurchaseInvoiceLines)
        {
            if (line.IdProduct is null || line.IdUnit is null) continue;

            var pu = await db.ProductUnit
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.IdProduct == line.IdProduct.Value && u.IdUnit == line.IdUnit.Value,
                    CancellationToken.None);

            decimal conversionFactor = pu?.ConversionFactor ?? 1m;
            decimal quantityBase     = Math.Round(line.Quantity * conversionFactor, 6);
            decimal unitCost         = conversionFactor > 0
                ? Math.Round(line.UnitPrice / conversionFactor, 6)
                : line.UnitPrice;

            line.QuantityBase = quantityBase;  // actualizar línea trackeada (L2 en confirm)

            var lot = new InventoryLot
            {
                IdProduct         = line.IdProduct.Value,
                LotNumber         = line.LotNumber,
                ExpirationDate    = line.ExpirationDate,
                UnitCost          = unitCost,
                QuantityAvailable = quantityBase,
                SourceType        = "Compra",
                IdPurchaseInvoice = invoice.IdPurchaseInvoice,
                IdWarehouse       = warehouseId,
                CreatedAt         = DateTime.UtcNow
            };

            db.InventoryLot.Add(lot);
            await db.SaveChangesAsync(CancellationToken.None);

            // Recalcular AverageCost (costo promedio ponderado)
            var product = await db.Product.FindAsync([line.IdProduct.Value], CancellationToken.None);
            var allLots = await db.InventoryLot
                .Where(il => il.IdProduct == line.IdProduct.Value)
                .ToListAsync(CancellationToken.None);

            var totalQty  = allLots.Sum(il => il.QuantityAvailable);
            var totalCost = allLots.Sum(il => il.QuantityAvailable * il.UnitCost);
            product!.AverageCost = totalQty > 0 ? Math.Round(totalCost / totalQty, 6) : 0m;
        }

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
    /// <summary>
    /// Mapea líneas del request a entidades PurchaseInvoiceLine.
    /// Calcula QuantityBase = Quantity × ConversionFactor (L2).
    /// Retorna error si no existe la ProductUnit para el par IdProduct+IdUnit.
    /// </summary>
    private async Task<(List<PurchaseInvoiceLine> Lines, string? Error)> MapLinesAsync(
        IReadOnlyList<PurchaseInvoiceLineRequest> lines, CancellationToken ct)
    {
        var result = new List<PurchaseInvoiceLine>(lines.Count);

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

            result.Add(new PurchaseInvoiceLine
            {
                IdProduct       = l.IdProduct,
                IdUnit          = l.IdUnit,
                LotNumber       = string.IsNullOrWhiteSpace(l.LotNumber) ? null : l.LotNumber.Trim(),
                ExpirationDate  = l.ExpirationDate,
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

    // ── Resolver contacto proveedor ───────────────────────────────────────────
    // Si se envía IdContact → lo usa directamente y toma el nombre del catálogo.
    // Si no se envía pero sí ProviderName → get-or-create con tipo "PRO".
    // Si no se envía ninguno → lanza excepción (al menos uno es obligatorio).
    private async Task<(int? IdContact, string ProviderName)> ResolveContactAsync(
        int? idContactRequest, string? providerNameRequest, CancellationToken ct)
    {
        if (idContactRequest.HasValue)
        {
            var contactName = await db.Contact
                .AsNoTracking()
                .Where(c => c.IdContact == idContactRequest.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException($"El contacto con IdContact={idContactRequest.Value} no existe.");

            return (idContactRequest.Value, contactName);
        }

        if (!string.IsNullOrWhiteSpace(providerNameRequest))
        {
            var contact = await contacts.GetOrCreateAsync(providerNameRequest.Trim(), "PRO", ct);
            return (contact.IdContact, contact.Name);
        }

        throw new InvalidOperationException("Debe enviar IdContact o ProviderName para registrar la factura de compra.");
    }
}
