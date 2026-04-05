using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.InventoryAdjustments.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.InventoryAdjustments;

public sealed class InventoryAdjustmentService(AppDbContext db) : IInventoryAdjustmentService
{
    private static InventoryAdjustmentResponse ToResponse(InventoryAdjustment ia) => new(
        ia.IdInventoryAdjustment,
        ia.IdFiscalPeriod,
        ia.IdInventoryAdjustmentType,
        ia.IdInventoryAdjustmentTypeNavigation.CodeInventoryAdjustmentType,
        ia.IdInventoryAdjustmentTypeNavigation.NameInventoryAdjustmentType,
        ia.IdCurrency,
        ia.IdCurrencyNavigation.CodeCurrency,
        ia.ExchangeRateValue,
        ia.NumberAdjustment,
        ia.DateAdjustment,
        ia.DescriptionAdjustment,
        ia.StatusAdjustment,
        ia.CreatedAt,
        ia.InventoryAdjustmentEntries.FirstOrDefault()?.IdAccountingEntry,
        ia.InventoryAdjustmentLines.Select(l => new InventoryAdjustmentLineResponse(
            l.IdInventoryAdjustmentLine,
            l.IdInventoryLot,
            l.IdInventoryLotNavigation.Product.NameProduct,
            l.IdInventoryLotNavigation.LotNumber,
            l.QuantityDelta,
            l.UnitCostNew,
            l.DescriptionLine)).ToList());

    private static IQueryable<InventoryAdjustment> WithIncludes(IQueryable<InventoryAdjustment> q)
        => q.Include(ia => ia.IdInventoryAdjustmentTypeNavigation)
            .Include(ia => ia.IdCurrencyNavigation)
            .Include(ia => ia.InventoryAdjustmentEntries)
            .Include(ia => ia.InventoryAdjustmentLines)
               .ThenInclude(l => l.IdInventoryLotNavigation)
               .ThenInclude(il => il.Product);

    public async Task<IReadOnlyList<InventoryAdjustmentResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .OrderByDescending(ia => ia.DateAdjustment)
            .ThenByDescending(ia => ia.IdInventoryAdjustment)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<InventoryAdjustmentResponse?> GetByIdAsync(int idInventoryAdjustment, CancellationToken ct = default)
    {
        var ia = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .FirstOrDefaultAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        return ia is null ? null : ToResponse(ia);
    }

    public async Task<InventoryAdjustmentResponse> CreateAsync(CreateInventoryAdjustmentRequest request, CancellationToken ct = default)
    {
        var adjustment = new InventoryAdjustment
        {
            IdFiscalPeriod            = request.IdFiscalPeriod,
            IdInventoryAdjustmentType = request.IdInventoryAdjustmentType,
            IdCurrency                = request.IdCurrency,
            ExchangeRateValue         = request.ExchangeRateValue,
            NumberAdjustment          = "BORRADOR",
            DateAdjustment            = request.DateAdjustment,
            DescriptionAdjustment     = request.DescriptionAdjustment,
            StatusAdjustment          = "Borrador",
            CreatedAt                 = DateTime.UtcNow,
            InventoryAdjustmentLines  = request.Lines.Select(l => new InventoryAdjustmentLine
            {
                IdInventoryLot  = l.IdInventoryLot,
                QuantityDelta   = l.QuantityDelta,
                UnitCostNew     = l.UnitCostNew,
                DescriptionLine = l.DescriptionLine
            }).ToList()
        };

        db.InventoryAdjustment.Add(adjustment);
        await db.SaveChangesAsync(ct);

        var created = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .FirstAsync(ia => ia.IdInventoryAdjustment == adjustment.IdInventoryAdjustment, ct);

        return ToResponse(created);
    }

    public async Task<InventoryAdjustmentResponse?> ConfirmAsync(int idInventoryAdjustment, CancellationToken ct = default)
    {
        var adjustment = await db.InventoryAdjustment
            .Include(ia => ia.IdInventoryAdjustmentTypeNavigation)
            .Include(ia => ia.IdCurrencyNavigation)
            .Include(ia => ia.InventoryAdjustmentLines)
                .ThenInclude(l => l.IdInventoryLotNavigation)
                    .ThenInclude(il => il.Product)
            .FirstOrDefaultAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        if (adjustment is null || adjustment.StatusAdjustment != "Borrador") return null;

        // Generar número de ajuste
        var date   = adjustment.DateAdjustment;
        var prefix = $"AJ-{date.Year:D4}{date.Month:D2}{date.Day:D2}";
        var count  = await db.InventoryAdjustment
            .CountAsync(ia => ia.NumberAdjustment.StartsWith(prefix) && ia.StatusAdjustment == "Confirmado", ct);

        adjustment.NumberAdjustment = $"{prefix}-{(count + 1):D3}";
        adjustment.StatusAdjustment = "Confirmado";

        // Aplicar deltas y actualizar costos
        foreach (var line in adjustment.InventoryAdjustmentLines)
        {
            var lot = line.IdInventoryLotNavigation;

            if (line.QuantityDelta != 0)
            {
                var newQty = lot.QuantityAvailable + line.QuantityDelta;
                if (newQty < 0)
                    throw new InvalidOperationException(
                        $"El lote {lot.IdInventoryLot} quedaría con stock negativo ({newQty}).");

                lot.QuantityAvailable = newQty;
            }

            if (line.UnitCostNew.HasValue)
                lot.UnitCost = line.UnitCostNew.Value;

            // Recalcular averageCost si el delta es positivo o se actualizó el costo
            if (line.QuantityDelta > 0 || line.UnitCostNew.HasValue)
            {
                var product = lot.Product;
                var allLots = await db.InventoryLot
                    .Where(il => il.IdProduct == product.IdProduct)
                    .ToListAsync(ct);

                var totalQty  = allLots.Sum(il => il.QuantityAvailable);
                var totalCost = allLots.Sum(il => il.QuantityAvailable * il.UnitCost);

                product.AverageCost = totalQty > 0 ? totalCost / totalQty : 0m;
            }
        }

        // Generar asiento contable si el tipo tiene cuentas configuradas
        var adjustmentType = adjustment.IdInventoryAdjustmentTypeNavigation;

        if (adjustmentType.IdAccountInventoryDefault is not null)
        {
            var entryLines = new List<AccountingEntryLine>();

            foreach (var line in adjustment.InventoryAdjustmentLines)
            {
                var lot = line.IdInventoryLotNavigation;

                if (line.QuantityDelta > 0)
                {
                    // Entrada: DR Inventario / CR Contrapartida de entrada
                    if (adjustmentType.IdAccountCounterpartEntry is null)
                        throw new InvalidOperationException(
                            $"El tipo de ajuste '{adjustmentType.NameInventoryAdjustmentType}' tiene cuenta de inventario configurada pero falta 'IdAccountCounterpartEntry' para registrar la entrada del lote {lot.IdInventoryLot}. Configure la cuenta en el tipo de ajuste.");

                    var amount = Math.Round(line.QuantityDelta * (line.UnitCostNew ?? lot.UnitCost), 2);

                    entryLines.Add(new AccountingEntryLine
                    {
                        IdAccount       = adjustmentType.IdAccountInventoryDefault.Value,
                        DebitAmount     = amount,
                        CreditAmount    = 0,
                        DescriptionLine = line.DescriptionLine ?? $"Entrada lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} — {lot.Product.NameProduct}"
                    });
                    entryLines.Add(new AccountingEntryLine
                    {
                        IdAccount       = adjustmentType.IdAccountCounterpartEntry.Value,
                        DebitAmount     = 0,
                        CreditAmount    = amount,
                        DescriptionLine = line.DescriptionLine ?? $"Entrada lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} — {lot.Product.NameProduct}"
                    });
                }
                else if (line.QuantityDelta < 0)
                {
                    // Salida: DR Contrapartida de salida / CR Inventario
                    if (adjustmentType.IdAccountCounterpartExit is null)
                        throw new InvalidOperationException(
                            $"El tipo de ajuste '{adjustmentType.NameInventoryAdjustmentType}' tiene cuenta de inventario configurada pero falta 'IdAccountCounterpartExit' para registrar la salida del lote {lot.IdInventoryLot}. Configure la cuenta en el tipo de ajuste.");

                    var amount = Math.Round(Math.Abs(line.QuantityDelta) * lot.UnitCost, 2);

                    entryLines.Add(new AccountingEntryLine
                    {
                        IdAccount       = adjustmentType.IdAccountCounterpartExit.Value,
                        DebitAmount     = amount,
                        CreditAmount    = 0,
                        DescriptionLine = line.DescriptionLine ?? $"Salida lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} — {lot.Product.NameProduct}"
                    });
                    entryLines.Add(new AccountingEntryLine
                    {
                        IdAccount       = adjustmentType.IdAccountInventoryDefault.Value,
                        DebitAmount     = 0,
                        CreditAmount    = amount,
                        DescriptionLine = line.DescriptionLine ?? $"Salida lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} — {lot.Product.NameProduct}"
                    });
                }
                else if (line.UnitCostNew.HasValue)
                {
                    // Ajuste de costo puro (QuantityDelta == 0)
                    var diffAmount = Math.Round(lot.QuantityAvailable * (line.UnitCostNew.Value - lot.UnitCost), 2);

                    if (diffAmount > 0)
                    {
                        // Aumento de costo: DR Inventario / CR Contrapartida entrada
                        if (adjustmentType.IdAccountCounterpartEntry is null)
                            throw new InvalidOperationException(
                                $"El tipo de ajuste '{adjustmentType.NameInventoryAdjustmentType}' requiere 'IdAccountCounterpartEntry' para registrar el aumento de costo del lote {lot.IdInventoryLot}.");

                        entryLines.Add(new AccountingEntryLine
                        {
                            IdAccount       = adjustmentType.IdAccountInventoryDefault.Value,
                            DebitAmount     = diffAmount,
                            CreditAmount    = 0,
                            DescriptionLine = line.DescriptionLine ?? $"Ajuste costo lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} — {lot.Product.NameProduct}"
                        });
                        entryLines.Add(new AccountingEntryLine
                        {
                            IdAccount       = adjustmentType.IdAccountCounterpartEntry.Value,
                            DebitAmount     = 0,
                            CreditAmount    = diffAmount,
                            DescriptionLine = line.DescriptionLine ?? $"Ajuste costo lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} — {lot.Product.NameProduct}"
                        });
                    }
                    else if (diffAmount < 0)
                    {
                        // Reducción de costo: DR Contrapartida salida / CR Inventario
                        if (adjustmentType.IdAccountCounterpartExit is null)
                            throw new InvalidOperationException(
                                $"El tipo de ajuste '{adjustmentType.NameInventoryAdjustmentType}' requiere 'IdAccountCounterpartExit' para registrar la reducción de costo del lote {lot.IdInventoryLot}.");

                        var absAmount = Math.Abs(diffAmount);
                        entryLines.Add(new AccountingEntryLine
                        {
                            IdAccount       = adjustmentType.IdAccountCounterpartExit.Value,
                            DebitAmount     = absAmount,
                            CreditAmount    = 0,
                            DescriptionLine = line.DescriptionLine ?? $"Ajuste costo lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} — {lot.Product.NameProduct}"
                        });
                        entryLines.Add(new AccountingEntryLine
                        {
                            IdAccount       = adjustmentType.IdAccountInventoryDefault.Value,
                            DebitAmount     = 0,
                            CreditAmount    = absAmount,
                            DescriptionLine = line.DescriptionLine ?? $"Ajuste costo lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} — {lot.Product.NameProduct}"
                        });
                    }
                    // diffAmount == 0: sin movimiento contable
                }
            }

            if (entryLines.Count > 0)
            {
                var entry = new AccountingEntry
                {
                    IdFiscalPeriod    = adjustment.IdFiscalPeriod,
                    IdCurrency        = adjustment.IdCurrency,
                    NumberEntry       = $"AJ-{adjustment.IdInventoryAdjustment:D6}",
                    DateEntry         = adjustment.DateAdjustment,
                    DescriptionEntry  = adjustment.DescriptionAdjustment ?? $"Ajuste de inventario {adjustment.NumberAdjustment}",
                    StatusEntry       = "Publicado",
                    ReferenceEntry    = adjustment.NumberAdjustment,
                    ExchangeRateValue = adjustment.ExchangeRateValue,
                    OriginModule      = "InventoryAdjustment",
                    IdOriginRecord    = adjustment.IdInventoryAdjustment,
                    CreatedAt         = DateTime.UtcNow
                };

                foreach (var line in entryLines) entry.AccountingEntryLines.Add(line);

                db.AccountingEntry.Add(entry);
                await db.SaveChangesAsync(CancellationToken.None);

                db.InventoryAdjustmentEntry.Add(new InventoryAdjustmentEntry
                {
                    IdInventoryAdjustment = adjustment.IdInventoryAdjustment,
                    IdAccountingEntry     = entry.IdAccountingEntry
                });
            }
        }

        await db.SaveChangesAsync(CancellationToken.None);

        var confirmed = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .FirstAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        return ToResponse(confirmed);
    }

    public async Task<(InventoryAdjustmentResponse? Result, string? ConflictMessage)> CancelAsync(
        int idInventoryAdjustment, CancellationToken ct = default)
    {
        var adjustment = await db.InventoryAdjustment
            .Include(ia => ia.InventoryAdjustmentLines)
                .ThenInclude(l => l.IdInventoryLotNavigation)
                    .ThenInclude(il => il.Product)
            .FirstOrDefaultAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        if (adjustment is null) return (null, null);
        if (adjustment.StatusAdjustment == "Anulado")
            return ((await GetByIdAsync(idInventoryAdjustment, ct)), null);
        if (adjustment.StatusAdjustment != "Confirmado")
            return (null, $"Solo se pueden anular ajustes confirmados. Estado actual: '{adjustment.StatusAdjustment}'.");

        // Verificar que la reversión no deje stock negativo
        foreach (var line in adjustment.InventoryAdjustmentLines)
        {
            if (line.QuantityDelta == 0) continue;  // ajuste de costo puro: sin cambio de cantidad

            var lot = line.IdInventoryLotNavigation;
            var newQty = lot.QuantityAvailable - line.QuantityDelta;
            if (newQty < 0)
                return (null,
                    $"No se puede anular el ajuste: el lote {lot.LotNumber ?? lot.IdInventoryLot.ToString()} " +
                    $"({lot.Product.NameProduct}) quedaría con stock negativo ({newQty:F4}) al revertir la cantidad {line.QuantityDelta:F4}. " +
                    $"Probablemente el lote ya tiene movimientos posteriores.");
        }

        // Revertir deltas de inventario y recalcular AverageCost
        foreach (var line in adjustment.InventoryAdjustmentLines)
        {
            if (line.QuantityDelta == 0) continue;

            var lot     = line.IdInventoryLotNavigation;
            var product = lot.Product;

            lot.QuantityAvailable -= line.QuantityDelta;

            // Recalcular WACC del producto
            var allLots = await db.InventoryLot
                .Where(il => il.IdProduct == product.IdProduct)
                .ToListAsync(CancellationToken.None);

            var totalQty  = allLots.Sum(il => il.QuantityAvailable);
            var totalCost = allLots.Sum(il => il.QuantityAvailable * il.UnitCost);
            product.AverageCost = totalQty > 0 ? Math.Round(totalCost / totalQty, 6) : 0m;
        }

        // Anular asientos contables vinculados
        var entryIds = await db.InventoryAdjustmentEntry
            .Where(iae => iae.IdInventoryAdjustment == idInventoryAdjustment)
            .Select(iae => iae.IdAccountingEntry)
            .ToListAsync(CancellationToken.None);

        if (entryIds.Count > 0)
            await db.AccountingEntry
                .Where(ae => entryIds.Contains(ae.IdAccountingEntry) && ae.StatusEntry != "Anulado")
                .ExecuteUpdateAsync(s => s.SetProperty(ae => ae.StatusEntry, "Anulado"), CancellationToken.None);

        adjustment.StatusAdjustment = "Anulado";
        await db.SaveChangesAsync(CancellationToken.None);

        var cancelled = await WithIncludes(db.InventoryAdjustment.AsNoTracking())
            .FirstAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, CancellationToken.None);

        return (ToResponse(cancelled), null);
    }

    public async Task<bool> DeleteAsync(int idInventoryAdjustment, CancellationToken ct = default)
    {
        var adjustment = await db.InventoryAdjustment
            .FirstOrDefaultAsync(ia => ia.IdInventoryAdjustment == idInventoryAdjustment, ct);

        if (adjustment is null) return false;
        if (adjustment.StatusAdjustment != "Borrador")
            throw new InvalidOperationException("Solo se pueden eliminar ajustes en estado Borrador.");

        db.InventoryAdjustment.Remove(adjustment);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
