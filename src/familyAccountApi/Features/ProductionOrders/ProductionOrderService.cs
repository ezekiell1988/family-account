using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductionOrders.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductionOrders;

public sealed class ProductionOrderService(AppDbContext db) : IProductionOrderService
{
    private static IQueryable<ProductionOrderResponse> Project(IQueryable<ProductionOrder> q) =>
        q.Select(po => new ProductionOrderResponse(
            po.IdProductionOrder,
            po.IdFiscalPeriod,
            po.IdSalesOrder,
            po.IdSalesOrderNavigation != null ? po.IdSalesOrderNavigation.NumberOrder : null,
            po.IdSalesOrderNavigation != null ? po.IdSalesOrderNavigation.IdContactNavigation.Name : null,
            po.NumberProductionOrder,
            po.DateOrder,
            po.DateRequired,
            po.StatusProductionOrder,
            po.DescriptionOrder,
            po.IdWarehouse,
            po.IdWarehouseNavigation != null ? po.IdWarehouseNavigation.NameWarehouse : null,
            po.CreatedAt,
            po.ProductionOrderLines.Select(l => new ProductionOrderLineResponse(
                l.IdProductionOrderLine,
                l.IdProduct,
                l.IdProductNavigation.NameProduct,
                l.IdProductUnit,
                l.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
                l.IdSalesOrderLine,
                l.QuantityRequired,
                l.QuantityProduced,
                l.QuantityRequired - l.QuantityProduced,
                l.DescriptionLine)).ToList()));

    public async Task<IReadOnlyList<ProductionOrderResponse>> GetAllAsync(CancellationToken ct = default) =>
        await Project(db.ProductionOrder.AsNoTracking().OrderByDescending(po => po.DateOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductionOrderResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default) =>
        await Project(db.ProductionOrder.AsNoTracking()
            .Where(po => po.IdFiscalPeriod == idFiscalPeriod)
            .OrderByDescending(po => po.DateOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductionOrderResponse>> GetBySalesOrderAsync(int idSalesOrder, CancellationToken ct = default) =>
        await Project(db.ProductionOrder.AsNoTracking()
            .Where(po => po.IdSalesOrder == idSalesOrder))
            .ToListAsync(ct);

    public async Task<ProductionOrderResponse?> GetByIdAsync(int idProductionOrder, CancellationToken ct = default) =>
        await Project(db.ProductionOrder.AsNoTracking()
            .Where(po => po.IdProductionOrder == idProductionOrder))
            .FirstOrDefaultAsync(ct);

    public async Task<ProductionOrderResponse> CreateAsync(CreateProductionOrderRequest request, CancellationToken ct = default)
    {
        var entity = new ProductionOrder
        {
            IdFiscalPeriod         = request.IdFiscalPeriod,
            IdSalesOrder           = request.IdSalesOrder,
            IdWarehouse            = request.IdWarehouse,
            NumberProductionOrder  = "BORRADOR",
            DateOrder              = request.DateOrder,
            DateRequired           = request.DateRequired,
            StatusProductionOrder  = "Borrador",
            DescriptionOrder       = request.DescriptionOrder,
            CreatedAt              = DateTime.UtcNow
        };

        foreach (var l in request.Lines)
        {
            entity.ProductionOrderLines.Add(new ProductionOrderLine
            {
                IdProduct        = l.IdProduct,
                IdProductUnit    = l.IdProductUnit,
                IdSalesOrderLine = l.IdSalesOrderLine,
                QuantityRequired = l.QuantityRequired,
                QuantityProduced = 0m,
                DescriptionLine  = l.DescriptionLine
            });
        }

        db.ProductionOrder.Add(entity);
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(entity.IdProductionOrder, ct))!;
    }

    public async Task<ProductionOrderResponse?> UpdateAsync(int idProductionOrder, UpdateProductionOrderRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductionOrder
            .Include(po => po.ProductionOrderLines)
            .FirstOrDefaultAsync(po => po.IdProductionOrder == idProductionOrder, ct);

        if (entity is null) return null;
        if (entity.StatusProductionOrder != "Borrador")
            throw new InvalidOperationException("Solo se puede editar una orden en estado Borrador.");

        db.ProductionOrderLine.RemoveRange(entity.ProductionOrderLines);

        entity.IdFiscalPeriod   = request.IdFiscalPeriod;
        entity.IdSalesOrder     = request.IdSalesOrder;
        entity.IdWarehouse      = request.IdWarehouse;
        entity.DateOrder        = request.DateOrder;
        entity.DateRequired     = request.DateRequired;
        entity.DescriptionOrder = request.DescriptionOrder;

        foreach (var l in request.Lines)
        {
            entity.ProductionOrderLines.Add(new ProductionOrderLine
            {
                IdProduct        = l.IdProduct,
                IdProductUnit    = l.IdProductUnit,
                IdSalesOrderLine = l.IdSalesOrderLine,
                QuantityRequired = l.QuantityRequired,
                QuantityProduced = 0m,
                DescriptionLine  = l.DescriptionLine
            });
        }

        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(idProductionOrder, ct))!;
    }

    public async Task<(bool Ok, string? Error, IReadOnlyList<string>? Warnings)> UpdateStatusAsync(int idProductionOrder, UpdateProductionOrderStatusRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductionOrder
            .Include(po => po.ProductionOrderLines)
                .ThenInclude(l => l.IdProductNavigation)
            .FirstOrDefaultAsync(po => po.IdProductionOrder == idProductionOrder, ct);

        if (entity is null) return (false, "Orden de producción no encontrada.", null);

        var current   = entity.StatusProductionOrder;
        var newStatus = request.StatusProductionOrder;

        var allowed = (current, newStatus) switch
        {
            ("Borrador",   "Pendiente")   => true,
            ("Pendiente",  "EnProceso")   => true,
            ("Pendiente",  "Completado")  => true,   // flujo automático: saltar EnProceso
            ("EnProceso",  "Completado")  => true,
            ("Pendiente",  "Anulado")     => true,
            ("EnProceso",  "Anulado")     => true,
            ("Borrador",   "Anulado")     => true,
            _ => false
        };

        if (!allowed) return (false, $"Transición inválida: {current} → {newStatus}.", null);

        // Al confirmar la orden (Borrador → Pendiente), asigna número correlativo
        if (current == "Borrador" && newStatus == "Pendiente")
        {
            var count = await db.ProductionOrder.CountAsync(po => po.StatusProductionOrder != "Borrador", ct);
            entity.NumberProductionOrder = $"OP-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
        }

        entity.StatusProductionOrder = newStatus;

        // ── Al completar: movimientos de inventario ───────────────────────
        if (newStatus == "Completado")
        {
            var (ok, error, warnings) = await CompleteProductionAsync(entity, request, ct);
            if (!ok) return (false, error, null);
            await db.SaveChangesAsync(ct);
            return (true, null, warnings);
        }

        await db.SaveChangesAsync(ct);
        return (true, null, null);
    }

    // ── Lógica de completado de producción ────────────────────────────────
    private async Task<(bool Ok, string? Error, IReadOnlyList<string> Warnings)> CompleteProductionAsync(
        ProductionOrder entity, UpdateProductionOrderStatusRequest request, CancellationToken ct)
    {
        var warehouseId = request.IdWarehouse ?? entity.IdWarehouse;
        if (warehouseId is null)
            return (false, "La orden no tiene bodega asignada. Envíe idWarehouse en el request o actualice la orden.", []);

        var warnings = new List<string>();

        var adjustmentType = await db.InventoryAdjustmentType
            .FirstAsync(t => t.CodeInventoryAdjustmentType == "PRODUCCION", ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var realQtyByLine = request.Lines?
            .ToDictionary(l => l.IdProductionOrderLine, l => l.QuantityProduced)
            ?? new Dictionary<int, decimal>();

        foreach (var line in entity.ProductionOrderLines)
        {
            var quantityProduced = realQtyByLine.TryGetValue(line.IdProductionOrderLine, out var rq)
                ? rq
                : line.QuantityRequired;

            var recipe = await db.ProductRecipe
                .Include(r => r.ProductRecipeLines)
                    .ThenInclude(rl => rl.IdProductInputNavigation)
                .FirstOrDefaultAsync(r => r.IdProductOutput == line.IdProduct && r.IsActive, ct);

            if (recipe is null)
            {
                warnings.Add($"Producto '{line.IdProductNavigation.NameProduct}' (ID={line.IdProduct}) no tiene receta activa. Sin movimiento de MP.");
                var ptLotNoRecipe = await CreateFinishedGoodsLotAsync(line.IdProduct, quantityProduced, 0m, warehouseId.Value, null, ct);
                await UpdateFulfillmentLotAsync(entity.IdProductionOrder, ptLotNoRecipe.IdInventoryLot, 0m, ct);
                line.QuantityProduced += quantityProduced;
                await db.SaveChangesAsync(ct);
                continue;
            }

            var factor = recipe.QuantityOutput > 0 ? quantityProduced / recipe.QuantityOutput : 1m;

            // Crear ajuste de inventario tipo PRODUCCION
            var adjPrefix = $"AJ-{today:yyyyMMdd}";
            var adjCount  = await db.InventoryAdjustment
                .CountAsync(ia => ia.NumberAdjustment.StartsWith(adjPrefix) && ia.StatusAdjustment == "Confirmado", ct);

            var adjustment = new InventoryAdjustment
            {
                IdFiscalPeriod            = entity.IdFiscalPeriod,
                IdInventoryAdjustmentType = adjustmentType.IdInventoryAdjustmentType,
                IdCurrency                = 1,
                ExchangeRateValue         = 1m,
                NumberAdjustment          = $"{adjPrefix}-{(adjCount + 1):D3}",
                DateAdjustment            = today,
                DescriptionAdjustment     = $"Consumo MP — {entity.NumberProductionOrder} — {line.IdProductNavigation.NameProduct}",
                StatusAdjustment          = "Confirmado",
                IdProductionOrder         = entity.IdProductionOrder,
                CreatedAt                 = DateTime.UtcNow
            };

            db.InventoryAdjustment.Add(adjustment);
            await db.SaveChangesAsync(ct);

            decimal totalMpCost   = 0m;
            var snapshotLines     = new List<ProductionSnapshotLine>();

            foreach (var recipeLine in recipe.ProductRecipeLines.OrderBy(r => r.SortOrder))
            {
                var qtyNeeded  = Math.Round(recipeLine.QuantityInput * factor, 4);
                var remaining  = qtyNeeded;

                // Consumir FEFO
                var lots = await db.InventoryLot
                    .Include(il => il.Product)
                    .Where(il => il.IdProduct   == recipeLine.IdProductInput
                              && il.IdWarehouse == warehouseId
                              && il.StatusLot   == "Disponible")
                    .OrderBy(il => il.ExpirationDate == null ? 1 : 0)
                    .ThenBy(il => il.ExpirationDate)
                    .ThenBy(il => il.IdInventoryLot)
                    .ToListAsync(ct);

                foreach (var lot in lots)
                {
                    if (remaining <= 0m) break;
                    var consume = Math.Min(remaining, lot.QuantityAvailable);
                    totalMpCost           += Math.Round(consume * lot.UnitCost, 6);
                    lot.QuantityAvailable -= consume;
                    remaining             -= consume;

                    adjustment.InventoryAdjustmentLines.Add(new InventoryAdjustmentLine
                    {
                        IdInventoryLot  = lot.IdInventoryLot,
                        QuantityDelta   = -consume,
                        DescriptionLine = $"Consumo MP {recipeLine.IdProductInputNavigation.NameProduct} — {entity.NumberProductionOrder}"
                    });
                }

                // Stock insuficiente: forzar negativo en el lote más antiguo
                if (remaining > 0m)
                {
                    warnings.Add($"Stock insuficiente de '{recipeLine.IdProductInputNavigation.NameProduct}' " +
                                 $"(ID={recipeLine.IdProductInput}): faltaron {remaining:0.####} unidades base.");

                    var debtLot = await db.InventoryLot
                        .Include(il => il.Product)
                        .Where(il => il.IdProduct == recipeLine.IdProductInput && il.IdWarehouse == warehouseId)
                        .OrderBy(il => il.IdInventoryLot)
                        .FirstOrDefaultAsync(ct);

                    if (debtLot is not null)
                    {
                        totalMpCost              += Math.Round(remaining * debtLot.UnitCost, 6);
                        debtLot.QuantityAvailable -= remaining;
                        adjustment.InventoryAdjustmentLines.Add(new InventoryAdjustmentLine
                        {
                            IdInventoryLot  = debtLot.IdInventoryLot,
                            QuantityDelta   = -remaining,
                            DescriptionLine = $"Deuda MP {recipeLine.IdProductInputNavigation.NameProduct} — {entity.NumberProductionOrder} (stock insuf.)"
                        });
                    }
                    // Si no existe ningún lote, el consumo queda sin respaldo contable (se avisa en la advertencia)
                }

                // qtyConsumed = lo que entrará en el snapshot (toda la cantidad planificada, incluyendo la deuda)
                snapshotLines.Add(new ProductionSnapshotLine
                {
                    IdProductRecipeLine = recipeLine.IdProductRecipeLine,
                    IdProductInput      = recipeLine.IdProductInput,
                    QuantityCalculated  = qtyNeeded,
                    QuantityReal        = qtyNeeded,   // siempre producimos la cantidad declarada
                    SortOrder           = recipeLine.SortOrder
                });
            }

            await db.SaveChangesAsync(ct);

            await GenerateAdjustmentEntryAsync(adjustment, adjustmentType, entity.IdFiscalPeriod, today, ct);

            // IAS 2.12 — Capitalizar costos de producción al inventario de producto terminado
            // DR [cta inventario PT]  /  CR [cta costos de producción (115)]
            await GenerateCapitalizationEntryAsync(adjustment, adjustmentType, line.IdProduct, totalMpCost, entity.IdFiscalPeriod, today, ct);

            // Costo unitario del PT
            var unitCostPt = quantityProduced > 0 ? Math.Round(totalMpCost / quantityProduced, 6) : 0m;

            // Crear lote del producto terminado y vincular al fulfillment del pedido
            var ptLot = await CreateFinishedGoodsLotAsync(
                line.IdProduct, quantityProduced, unitCostPt, warehouseId.Value,
                adjustment.IdInventoryAdjustment, ct);
            await UpdateFulfillmentLotAsync(entity.IdProductionOrder, ptLot.IdInventoryLot, unitCostPt, ct);

            // Recalcular WAC del producto terminado
            var ptProduct = await db.Product.FirstAsync(p => p.IdProduct == line.IdProduct, ct);
            var allPtLots = await db.InventoryLot
                .Where(il => il.IdProduct == line.IdProduct)
                .ToListAsync(ct);

            var totalQty  = allPtLots.Sum(il => il.QuantityAvailable);
            var totalCost = allPtLots.Sum(il => il.QuantityAvailable * il.UnitCost);
            ptProduct.AverageCost = totalQty > 0 ? Math.Round(totalCost / totalQty, 6) : unitCostPt;

            line.QuantityProduced += quantityProduced;

            // Crear ProductionSnapshot
            var snapshot = new ProductionSnapshot
            {
                IdInventoryAdjustment = adjustment.IdInventoryAdjustment,
                IdProductRecipe       = recipe.IdProductRecipe,
                QuantityCalculated    = Math.Round(recipe.QuantityOutput * factor, 4),
                QuantityReal          = quantityProduced,
                CreatedAt             = DateTime.UtcNow
            };
            foreach (var sl in snapshotLines) snapshot.ProductionSnapshotLines.Add(sl);
            db.ProductionSnapshot.Add(snapshot);
            await db.SaveChangesAsync(ct);
        }

        return (true, null, warnings);
    }

    private async Task UpdateFulfillmentLotAsync(
        int idProductionOrder, int idInventoryLot, decimal unitCost, CancellationToken ct)
    {
        var fulfillment = await db.SalesOrderLineFulfillment
            .FirstOrDefaultAsync(f => f.IdProductionOrder == idProductionOrder
                                   && f.FulfillmentType   == "Produccion", ct);
        if (fulfillment is not null)
        {
            fulfillment.IdInventoryLot = idInventoryLot;
            fulfillment.UnitCost       = unitCost;
        }
    }

    private async Task<InventoryLot> CreateFinishedGoodsLotAsync(
        int idProduct, decimal quantity, decimal unitCost, int idWarehouse,
        int? idInventoryAdjustment, CancellationToken ct)
    {
        var lot = new InventoryLot
        {
            IdProduct             = idProduct,
            UnitCost              = unitCost,
            QuantityAvailable     = quantity,
            QuantityReserved      = 0m,
            StatusLot             = "Disponible",
            SourceType            = "Producción",
            IdInventoryAdjustment = idInventoryAdjustment,
            IdWarehouse           = idWarehouse,
            CreatedAt             = DateTime.UtcNow
        };
        db.InventoryLot.Add(lot);
        await db.SaveChangesAsync(ct);
        return lot;
    }

    private async Task GenerateCapitalizationEntryAsync(
        InventoryAdjustment adjustment, InventoryAdjustmentType adjustmentType,
        int idProduct, decimal totalCost,
        int idFiscalPeriod, DateOnly date, CancellationToken ct)
    {
        if (totalCost == 0m) return;
        if (adjustmentType.IdAccountCounterpartExit is null) return;

        // Cuenta de inventario PT: ProductAccount del producto, o fallback al default del tipo de ajuste
        var productAccountEntry = await db.ProductAccount
            .Where(pa => pa.IdProduct == idProduct)
            .OrderBy(pa => pa.IdProductAccount)
            .FirstOrDefaultAsync(ct);

        var ptInventoryAccount = productAccountEntry?.IdAccount
            ?? adjustmentType.IdAccountInventoryDefault;

        if (ptInventoryAccount is null) return;

        var desc = $"IAS 2 — Capitalización MP→PT — {adjustment.NumberAdjustment}";

        var entry = new AccountingEntry
        {
            IdFiscalPeriod    = idFiscalPeriod,
            IdCurrency        = adjustment.IdCurrency,
            NumberEntry       = $"PROD-CAP-{adjustment.IdInventoryAdjustment:D6}",
            DateEntry         = date,
            DescriptionEntry  = desc,
            StatusEntry       = "Publicado",
            ReferenceEntry    = adjustment.NumberAdjustment,
            ExchangeRateValue = adjustment.ExchangeRateValue,
            OriginModule      = "ProductionOrder",
            IdOriginRecord    = adjustment.IdProductionOrder,
            CreatedAt         = DateTime.UtcNow
        };

        // DR: Inventario producto terminado
        entry.AccountingEntryLines.Add(new AccountingEntryLine
        {
            IdAccount       = ptInventoryAccount.Value,
            DebitAmount     = Math.Round(totalCost, 2),
            CreditAmount    = 0m,
            DescriptionLine = desc
        });

        // CR: Costos de Producción (115) — saldo queda en cero conforme a IAS 2.12
        entry.AccountingEntryLines.Add(new AccountingEntryLine
        {
            IdAccount       = adjustmentType.IdAccountCounterpartExit.Value,
            DebitAmount     = 0m,
            CreditAmount    = Math.Round(totalCost, 2),
            DescriptionLine = desc
        });

        db.AccountingEntry.Add(entry);
        await db.SaveChangesAsync(ct);

        db.InventoryAdjustmentEntry.Add(new InventoryAdjustmentEntry
        {
            IdInventoryAdjustment = adjustment.IdInventoryAdjustment,
            IdAccountingEntry     = entry.IdAccountingEntry
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task GenerateAdjustmentEntryAsync(
        InventoryAdjustment adjustment, InventoryAdjustmentType adjustmentType,
        int idFiscalPeriod, DateOnly date, CancellationToken ct)
    {
        if (adjustmentType.IdAccountInventoryDefault is null) return;

        var adjWithLots = await db.InventoryAdjustment
            .Include(ia => ia.InventoryAdjustmentLines)
                .ThenInclude(l => l.IdInventoryLotNavigation!)
                    .ThenInclude(il => il.Product)
            .FirstAsync(ia => ia.IdInventoryAdjustment == adjustment.IdInventoryAdjustment, ct);

        var productIds = adjWithLots.InventoryAdjustmentLines
            .Where(l => l.QuantityDelta < 0)
            .Select(l => l.IdInventoryLotNavigation!.IdProduct)
            .Distinct()
            .ToList();

        var productAccountList = await db.ProductAccount
            .Where(pa => productIds.Contains(pa.IdProduct))
            .Select(pa => new { pa.IdProduct, pa.IdAccount })
            .ToListAsync(ct);

        var productAccounts = productAccountList
            .GroupBy(pa => pa.IdProduct)
            .ToDictionary(g => g.Key, g => g.Min(x => x.IdAccount));

        var entryLines = new List<AccountingEntryLine>();

        foreach (var line in adjWithLots.InventoryAdjustmentLines.Where(l => l.QuantityDelta < 0))
        {
            var lot    = line.IdInventoryLotNavigation!;
            var amount = Math.Round(Math.Abs(line.QuantityDelta) * lot.UnitCost, 2);
            if (amount == 0m || adjustmentType.IdAccountCounterpartExit is null) continue;

            var desc = line.DescriptionLine ?? $"Consumo MP — {lot.Product.NameProduct}";

            entryLines.Add(new AccountingEntryLine
            {
                IdAccount       = adjustmentType.IdAccountCounterpartExit.Value,
                DebitAmount     = amount,
                CreditAmount    = 0m,
                DescriptionLine = desc
            });

            var crAccount = productAccounts.TryGetValue(lot.IdProduct, out var paAccount)
                ? paAccount
                : adjustmentType.IdAccountInventoryDefault!.Value;

            entryLines.Add(new AccountingEntryLine
            {
                IdAccount       = crAccount,
                DebitAmount     = 0m,
                CreditAmount    = amount,
                DescriptionLine = desc
            });
        }

        if (entryLines.Count == 0) return;

        var entry = new AccountingEntry
        {
            IdFiscalPeriod    = idFiscalPeriod,
            IdCurrency        = adjustment.IdCurrency,
            NumberEntry       = $"AJ-{adjustment.IdInventoryAdjustment:D6}",
            DateEntry         = date,
            DescriptionEntry  = adjustment.DescriptionAdjustment ?? $"Ajuste producción {adjustment.NumberAdjustment}",
            StatusEntry       = "Publicado",
            ReferenceEntry    = adjustment.NumberAdjustment,
            ExchangeRateValue = adjustment.ExchangeRateValue,
            OriginModule      = "ProductionOrder",
            IdOriginRecord    = adjustment.IdProductionOrder,
            CreatedAt         = DateTime.UtcNow
        };

        foreach (var l in entryLines) entry.AccountingEntryLines.Add(l);
        db.AccountingEntry.Add(entry);
        await db.SaveChangesAsync(ct);

        db.InventoryAdjustmentEntry.Add(new InventoryAdjustmentEntry
        {
            IdInventoryAdjustment = adjustment.IdInventoryAdjustment,
            IdAccountingEntry     = entry.IdAccountingEntry
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int idProductionOrder, CancellationToken ct = default)
    {
        var entity = await db.ProductionOrder.FindAsync([idProductionOrder], ct);
        if (entity is null) return false;
        if (entity.StatusProductionOrder != "Borrador")
            throw new InvalidOperationException("Solo se puede eliminar una orden en estado Borrador.");

        var deleted = await db.ProductionOrder
            .Where(po => po.IdProductionOrder == idProductionOrder)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }
}
