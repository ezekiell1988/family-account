using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.SalesOrders.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.SalesOrders;

public sealed class SalesOrderService(AppDbContext db) : ISalesOrderService
{
    // ── Proyección reutilizable ──────────────────────────────────────────────
    private static SalesOrderLineResponse MapLine(SalesOrderLine l) => new(
        l.IdSalesOrderLine,
        l.IdProduct,
        l.IdProductNavigation.NameProduct,
        l.IdProductUnit,
        l.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
        l.IdPriceListItem,
        l.Quantity,
        l.QuantityBase,
        l.UnitPrice,
        l.TaxPercent,
        l.TotalLineAmount,
        l.DescriptionLine,
        l.SalesOrderLineOptions.Select(o => new SalesOrderLineOptionResponse(
            o.IdSalesOrderLineOption,
            o.IdProductOptionItem,
            o.IdProductOptionItemNavigation.NameItem,
            o.IdProductOptionItemNavigation.PriceDelta,
            o.Quantity)).ToList());

    private static IQueryable<SalesOrderResponse> ProjectOrder(IQueryable<SalesOrder> q) =>
        q.Select(so => new SalesOrderResponse(
            so.IdSalesOrder,
            so.IdFiscalPeriod,
            so.IdCurrency,
            so.IdCurrencyNavigation.CodeCurrency,
            so.IdContact,
            so.IdContactNavigation.Name,
            so.IdPriceList,
            so.IdPriceListNavigation != null ? so.IdPriceListNavigation.NamePriceList : null,
            so.NumberOrder,
            so.DateOrder,
            so.DateDelivery,
            so.SubTotalAmount,
            so.TaxAmount,
            so.TotalAmount,
            so.ExchangeRateValue,
            so.StatusOrder,
            so.DescriptionOrder,
            so.CreatedAt,
            so.SalesOrderLines.Select(l => new SalesOrderLineResponse(
                l.IdSalesOrderLine,
                l.IdProduct,
                l.IdProductNavigation.NameProduct,
                l.IdProductUnit,
                l.IdProductUnitNavigation.UnitOfMeasure.NameUnit,
                l.IdPriceListItem,
                l.Quantity,
                l.QuantityBase,
                l.UnitPrice,
                l.TaxPercent,
                l.TotalLineAmount,
                l.DescriptionLine,
                l.SalesOrderLineOptions.Select(o => new SalesOrderLineOptionResponse(
                    o.IdSalesOrderLineOption,
                    o.IdProductOptionItem,
                    o.IdProductOptionItemNavigation.NameItem,
                    o.IdProductOptionItemNavigation.PriceDelta,
                    o.Quantity)).ToList())).ToList()));

    // ── Lecturas ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesOrderResponse>> GetAllAsync(CancellationToken ct = default) =>
        await ProjectOrder(db.SalesOrder.AsNoTracking().OrderByDescending(so => so.DateOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SalesOrderResponse>> GetByFiscalPeriodAsync(int idFiscalPeriod, CancellationToken ct = default) =>
        await ProjectOrder(db.SalesOrder.AsNoTracking()
            .Where(so => so.IdFiscalPeriod == idFiscalPeriod)
            .OrderByDescending(so => so.DateOrder))
            .ToListAsync(ct);

    public async Task<SalesOrderResponse?> GetByIdAsync(int idSalesOrder, CancellationToken ct = default) =>
        await ProjectOrder(db.SalesOrder.AsNoTracking()
            .Where(so => so.IdSalesOrder == idSalesOrder))
            .FirstOrDefaultAsync(ct);

    // ── Creates / Updates ────────────────────────────────────────────────────

    public async Task<(SalesOrderResponse? Result, string? Error)> CreateAsync(CreateSalesOrderRequest request, CancellationToken ct = default)
    {
        var optionError = await ValidateOptionsAsync(request.Lines, ct);
        if (optionError is not null) return (null, optionError);

        var entity = BuildOrder(request);

        await PopulateOptionsAsync(entity.SalesOrderLines, request.Lines, ct);
        RecalcLineAmountsWithOptions(entity);
        RecalcTotals(entity);

        db.SalesOrder.Add(entity);
        await db.SaveChangesAsync(ct);
        return ((await GetByIdAsync(entity.IdSalesOrder, ct))!, null);
    }

    public async Task<(SalesOrderResponse? Result, string? Error)> UpdateAsync(int idSalesOrder, UpdateSalesOrderRequest request, CancellationToken ct = default)
    {
        var entity = await db.SalesOrder
            .Include(so => so.SalesOrderLines)
                .ThenInclude(l => l.SalesOrderLineOptions)
            .FirstOrDefaultAsync(so => so.IdSalesOrder == idSalesOrder, ct);

        if (entity is null) return (null, null);
        if (entity.StatusOrder != "Borrador")
            return (null, "Solo se puede editar un pedido en estado Borrador.");

        var optionError = await ValidateOptionsAsync(request.Lines, ct);
        if (optionError is not null) return (null, optionError);

        db.SalesOrderLine.RemoveRange(entity.SalesOrderLines);

        entity.IdFiscalPeriod    = request.IdFiscalPeriod;
        entity.IdCurrency        = request.IdCurrency;
        entity.IdContact         = request.IdContact;
        entity.IdPriceList       = request.IdPriceList;
        entity.DateOrder         = request.DateOrder;
        entity.DateDelivery      = request.DateDelivery;
        entity.ExchangeRateValue = request.ExchangeRateValue;
        entity.DescriptionOrder  = request.DescriptionOrder;

        ApplyLines(entity, request.Lines);
        await PopulateOptionsAsync(entity.SalesOrderLines, request.Lines, ct);
        RecalcLineAmountsWithOptions(entity);
        RecalcTotals(entity);

        await db.SaveChangesAsync(ct);
        return ((await GetByIdAsync(idSalesOrder, ct))!, null);
    }

    // ── Estado ───────────────────────────────────────────────────────────────

    public async Task<(bool Ok, string? Error)> ConfirmAsync(int idSalesOrder, CancellationToken ct = default)
    {
        var entity = await db.SalesOrder
            .Include(s => s.SalesOrderLines)
            .FirstOrDefaultAsync(s => s.IdSalesOrder == idSalesOrder, ct);
        if (entity is null) return (false, "Pedido no encontrado.");
        if (entity.StatusOrder != "Borrador") return (false, "Solo se puede confirmar un pedido en estado Borrador.");

        // Validar que ninguna línea use un producto padre con variantes
        var productIds = entity.SalesOrderLines
            .Select(l => l.IdProduct)
            .Distinct()
            .ToList();

        if (productIds.Count > 0)
        {
            var parentProducts = await db.Product
                .AsNoTracking()
                .Where(p => productIds.Contains(p.IdProduct) && p.IsVariantParent)
                .Select(p => p.NameProduct)
                .ToListAsync(ct);

            if (parentProducts.Count > 0)
                return (false,
                    $"El pedido contiene producto(s) padre con variantes: {string.Join(", ", parentProducts)}. Debe seleccionar una variante específica en cada línea.");
        }

        entity.StatusOrder = "Confirmado";
        entity.NumberOrder = await GenerateNumberAsync(ct);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(int idSalesOrder, CancellationToken ct = default)
    {
        var entity = await db.SalesOrder.FindAsync([idSalesOrder], ct);
        if (entity is null) return (false, "Pedido no encontrado.");
        if (entity.StatusOrder == "Anulado") return (false, "El pedido ya está anulado.");
        if (entity.StatusOrder == "Completado") return (false, "No se puede anular un pedido Completado.");

        entity.StatusOrder = "Anulado";
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int idSalesOrder, CancellationToken ct = default)
    {
        var entity = await db.SalesOrder.FindAsync([idSalesOrder], ct);
        if (entity is null) return false;
        if (entity.StatusOrder != "Borrador")
            throw new InvalidOperationException("Solo se puede eliminar un pedido en estado Borrador.");

        var deleted = await db.SalesOrder.Where(so => so.IdSalesOrder == idSalesOrder).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    // ── Fulfillments ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesOrderFulfillmentResponse>> GetFulfillmentsAsync(int idSalesOrder, CancellationToken ct = default) =>
        await db.SalesOrderLineFulfillment
            .AsNoTracking()
            .Where(f => f.IdSalesOrderLineNavigation.IdSalesOrder == idSalesOrder)
            .Select(f => new SalesOrderFulfillmentResponse(
                f.IdSalesOrderLineFulfillment,
                f.IdSalesOrderLine,
                f.FulfillmentType,
                f.IdInventoryLot,
                f.IdProductionOrder,
                f.IdProductionOrderNavigation != null ? f.IdProductionOrderNavigation.NumberProductionOrder : null,
                f.QuantityBase,
                f.UnitCost,
                f.CreatedAt))
            .ToListAsync(ct);

    public async Task<(SalesOrderFulfillmentResponse? Result, string? Error)> AddFulfillmentAsync(int idSalesOrder, AddFulfillmentRequest request, CancellationToken ct = default)
    {
        var line = await db.SalesOrderLine
            .FirstOrDefaultAsync(l => l.IdSalesOrderLine == request.IdSalesOrderLine && l.IdSalesOrder == idSalesOrder, ct);
        if (line is null) return (null, "Línea no encontrada en el pedido.");

        if (request.FulfillmentType == "Stock" && request.IdInventoryLot is null)
            return (null, "Para FulfillmentType 'Stock' debe indicarse IdInventoryLot.");
        if (request.FulfillmentType == "Produccion" && request.IdProductionOrder is null)
            return (null, "Para FulfillmentType 'Produccion' debe indicarse IdProductionOrder.");

        // ── Reserva de stock (M3) ────────────────────────────────────────────
        if (request.FulfillmentType == "Stock" && request.IdInventoryLot.HasValue)
        {
            var lot = await db.InventoryLot.FindAsync([request.IdInventoryLot.Value], ct);
            if (lot is null)
                return (null, "El lote de inventario indicado no existe.");

            decimal netAvailable = lot.QuantityAvailable - lot.QuantityReserved;
            if (netAvailable < request.QuantityBase)
                return (null, $"Stock neto insuficiente en el lote '{lot.LotNumber ?? lot.IdInventoryLot.ToString()}'. Disponible neto: {netAvailable:N4}, solicitado: {request.QuantityBase:N4}.");

            lot.QuantityReserved += request.QuantityBase;
        }

        var entity = new SalesOrderLineFulfillment
        {
            IdSalesOrderLine = request.IdSalesOrderLine,
            FulfillmentType  = request.FulfillmentType,
            IdInventoryLot   = request.IdInventoryLot,
            IdProductionOrder = request.IdProductionOrder,
            QuantityBase     = request.QuantityBase,
            CreatedAt        = DateTime.UtcNow
        };

        db.SalesOrderLineFulfillment.Add(entity);
        await db.SaveChangesAsync(ct);

        var result = await db.SalesOrderLineFulfillment
            .AsNoTracking()
            .Where(f => f.IdSalesOrderLineFulfillment == entity.IdSalesOrderLineFulfillment)
            .Select(f => new SalesOrderFulfillmentResponse(
                f.IdSalesOrderLineFulfillment,
                f.IdSalesOrderLine,
                f.FulfillmentType,
                f.IdInventoryLot,
                f.IdProductionOrder,
                f.IdProductionOrderNavigation != null ? f.IdProductionOrderNavigation.NumberProductionOrder : null,
                f.QuantityBase,
                f.UnitCost,
                f.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return (result, null);
    }

    public async Task<bool> RemoveFulfillmentAsync(int idSalesOrderLineFulfillment, CancellationToken ct = default)
    {
        var fulfillment = await db.SalesOrderLineFulfillment
            .FirstOrDefaultAsync(f => f.IdSalesOrderLineFulfillment == idSalesOrderLineFulfillment, ct);

        if (fulfillment is null) return false;

        // ── Liberar reserva de stock (M3) ────────────────────────────────────
        if (fulfillment.FulfillmentType == "Stock" && fulfillment.IdInventoryLot.HasValue)
        {
            var lot = await db.InventoryLot.FindAsync([fulfillment.IdInventoryLot.Value], ct);
            if (lot is not null)
                lot.QuantityReserved = Math.Max(0m, lot.QuantityReserved - fulfillment.QuantityBase);
        }

        db.SalesOrderLineFulfillment.Remove(fulfillment);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Advances ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesOrderAdvanceResponse>> GetAdvancesAsync(int idSalesOrder, CancellationToken ct = default) =>
        await db.SalesOrderAdvance
            .AsNoTracking()
            .Where(a => a.IdSalesOrder == idSalesOrder)
            .Select(a => new SalesOrderAdvanceResponse(
                a.IdSalesOrderAdvance,
                a.IdSalesOrder,
                a.IdAccountingEntry,
                a.IdProductionOrder,
                a.IdProductionOrderNavigation != null ? a.IdProductionOrderNavigation.NumberProductionOrder : null,
                a.Amount,
                a.DateAdvance,
                a.DescriptionAdvance,
                a.CreatedAt))
            .ToListAsync(ct);

    public async Task<(SalesOrderAdvanceResponse? Result, string? Error)> AddAdvanceAsync(int idSalesOrder, CreateSalesOrderAdvanceRequest request, CancellationToken ct = default)
    {
        var order = await db.SalesOrder.FindAsync([idSalesOrder], ct);
        if (order is null) return (null, "Pedido no encontrado.");

        var entity = new SalesOrderAdvance
        {
            IdSalesOrder       = idSalesOrder,
            IdAccountingEntry  = request.IdAccountingEntry,
            IdProductionOrder  = request.IdProductionOrder,
            Amount             = request.Amount,
            DateAdvance        = request.DateAdvance,
            DescriptionAdvance = request.DescriptionAdvance,
            CreatedAt          = DateTime.UtcNow
        };

        db.SalesOrderAdvance.Add(entity);
        await db.SaveChangesAsync(ct);

        var result = await db.SalesOrderAdvance
            .AsNoTracking()
            .Where(a => a.IdSalesOrderAdvance == entity.IdSalesOrderAdvance)
            .Select(a => new SalesOrderAdvanceResponse(
                a.IdSalesOrderAdvance,
                a.IdSalesOrder,
                a.IdAccountingEntry,
                a.IdProductionOrder,
                a.IdProductionOrderNavigation != null ? a.IdProductionOrderNavigation.NumberProductionOrder : null,
                a.Amount,
                a.DateAdvance,
                a.DescriptionAdvance,
                a.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return (result, null);
    }

    public async Task<bool> RemoveAdvanceAsync(int idSalesOrderAdvance, CancellationToken ct = default)
    {
        var deleted = await db.SalesOrderAdvance
            .Where(a => a.IdSalesOrderAdvance == idSalesOrderAdvance)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    // ── Helpers privados ─────────────────────────────────────────────────────

    /// <summary>T9: valida que los option items pertenezcan al producto de cada línea,
    /// sin duplicados dentro del mismo grupo. T10: valida availability rules.</summary>
    private async Task<string?> ValidateOptionsAsync(IReadOnlyList<SalesOrderLineRequest> lines, CancellationToken ct)
    {
        for (var idx = 0; idx < lines.Count; idx++)
        {
            var line = lines[idx];
            if (line.Options is null || line.Options.Count == 0) continue;

            var requestedIds = line.Options.Select(o => o.IdProductOptionItem).ToList();

            // Cargar los option items con su grupo (filtrando por producto)
            var items = await db.ProductOptionItem
                .AsNoTracking()
                .Include(i => i.IdProductOptionGroupNavigation)
                .Where(i => requestedIds.Contains(i.IdProductOptionItem)
                         && i.IdProductOptionGroupNavigation.IdProduct == line.IdProduct)
                .ToListAsync(ct);

            // T9-a: todos los ids deben pertenecer al producto
            var foundIds  = items.Select(i => i.IdProductOptionItem).ToHashSet();
            var invalidIds = requestedIds.Where(id => !foundIds.Contains(id)).ToList();
            if (invalidIds.Count > 0)
                return $"Línea {idx + 1}: los siguientes option items no pertenecen al producto: {string.Join(", ", invalidIds)}.";

            // T9-b: sin duplicados dentro del mismo grupo (máximo 1 item por grupo por línea)
            var dupGroup = items
                .GroupBy(i => i.IdProductOptionGroup)
                .FirstOrDefault(g => g.Count() > 1 && line.Options.Count(o => g.Any(i => i.IdProductOptionItem == o.IdProductOptionItem)) > 1);
            if (dupGroup is not null)
                return $"Línea {idx + 1}: el grupo '{dupGroup.First().IdProductOptionGroupNavigation.NameGroup}' tiene más de un ítem seleccionado.";

            // T10: availability rules — cargar reglas de los items solicitados
            var availabilityRules = await db.ProductOptionItemAvailability
                .AsNoTracking()
                .Where(r => requestedIds.Contains(r.IdRestrictedItem))
                .ToListAsync(ct);

            foreach (var rule in availabilityRules.GroupBy(r => r.IdRestrictedItem))
            {
                var enablerIds = rule.Select(r => r.IdEnablingItem).ToHashSet();
                var anEnabler  = requestedIds.Any(id => enablerIds.Contains(id));
                if (!anEnabler)
                    return $"Línea {idx + 1}: el ítem de opción {rule.Key} requiere que al menos uno de sus habilitadores esté seleccionado: {string.Join(", ", enablerIds)}.";
            }
        }

        return null;
    }

    /// <summary>T11: carga los option items y los asigna a las líneas del pedido,
    /// ajustando UnitPrice con la suma de PriceDeltas.</summary>
    private async Task PopulateOptionsAsync(
        ICollection<SalesOrderLine> salesLines,
        IReadOnlyList<SalesOrderLineRequest> requestLines,
        CancellationToken ct)
    {
        var linesArray = salesLines.ToArray();

        for (var i = 0; i < requestLines.Count; i++)
        {
            var reqLine  = requestLines[i];
            var saleLine = linesArray[i];

            if (reqLine.Options is null || reqLine.Options.Count == 0) continue;

            var itemIds = reqLine.Options.Select(o => o.IdProductOptionItem).Distinct().ToList();
            var itemMap = await db.ProductOptionItem
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.IdProductOptionItem))
                .ToDictionaryAsync(x => x.IdProductOptionItem, ct);

            foreach (var opt in reqLine.Options)
            {
                saleLine.SalesOrderLineOptions.Add(new SalesOrderLineOption
                {
                    IdProductOptionItem = opt.IdProductOptionItem,
                    Quantity            = opt.Quantity
                });
            }

            // Ajustar UnitPrice con suma de PriceDeltas
            var deltaSum = reqLine.Options.Sum(o =>
                itemMap.TryGetValue(o.IdProductOptionItem, out var item) ? item.PriceDelta * o.Quantity : 0m);

            saleLine.UnitPrice = reqLine.UnitPrice + deltaSum;
        }
    }

    /// <summary>Recalcula TotalLineAmount por línea después de aplicar las opciones.</summary>
    private static void RecalcLineAmountsWithOptions(SalesOrder order)
    {
        foreach (var line in order.SalesOrderLines)
            line.TotalLineAmount = Math.Round(line.Quantity * line.UnitPrice * (1 + line.TaxPercent / 100m), 2);
    }

    private static SalesOrder BuildOrder(CreateSalesOrderRequest request)
    {
        var order = new SalesOrder
        {
            IdFiscalPeriod    = request.IdFiscalPeriod,
            IdCurrency        = request.IdCurrency,
            IdContact         = request.IdContact,
            IdPriceList       = request.IdPriceList,
            NumberOrder       = "BORRADOR",
            DateOrder         = request.DateOrder,
            DateDelivery      = request.DateDelivery,
            ExchangeRateValue = request.ExchangeRateValue,
            StatusOrder       = "Borrador",
            DescriptionOrder  = request.DescriptionOrder,
            CreatedAt         = DateTime.UtcNow
        };

        ApplyLines(order, request.Lines);
        RecalcTotals(order);
        return order;
    }

    private static void ApplyLines(SalesOrder order, IReadOnlyList<SalesOrderLineRequest> lines)
    {
        foreach (var l in lines)
        {
            order.SalesOrderLines.Add(new SalesOrderLine
            {
                IdProduct       = l.IdProduct,
                IdProductUnit   = l.IdProductUnit,
                IdPriceListItem = l.IdPriceListItem,
                Quantity        = l.Quantity,
                QuantityBase    = l.Quantity,   // El ConversionFactor real se puede aplicar al confirmar
                UnitPrice       = l.UnitPrice,
                TaxPercent      = l.TaxPercent,
                TotalLineAmount = Math.Round(l.Quantity * l.UnitPrice * (1 + l.TaxPercent / 100m), 2),
                DescriptionLine = l.DescriptionLine
            });
        }
    }

    private static void RecalcTotals(SalesOrder order)
    {
        var subTotal = order.SalesOrderLines.Sum(l => Math.Round(l.Quantity * l.UnitPrice, 2));
        var taxTotal = order.SalesOrderLines.Sum(l => Math.Round(l.Quantity * l.UnitPrice * l.TaxPercent / 100m, 2));
        order.SubTotalAmount = subTotal;
        order.TaxAmount      = taxTotal;
        order.TotalAmount    = subTotal + taxTotal;
    }

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var count = await db.SalesOrder.CountAsync(so => so.StatusOrder != "Borrador", ct);
        return $"PED-{DateTime.UtcNow:yyyy}-{(count + 1):D4}";
    }

    // ── C5: Flujo de pedido configurado ──────────────────────────────────────

    public async Task<(SendToProductionResponse? Result, string? Error)> SendToProductionAsync(
        int idSalesOrder, CancellationToken ct = default)
    {
        // 1. Cargar pedido con líneas, opciones y producto
        var order = await db.SalesOrder
            .Include(so => so.SalesOrderLines)
                .ThenInclude(l => l.IdProductNavigation)
            .Include(so => so.SalesOrderLines)
                .ThenInclude(l => l.SalesOrderLineOptions)
                    .ThenInclude(o => o.IdProductOptionItemNavigation)
            .FirstOrDefaultAsync(so => so.IdSalesOrder == idSalesOrder, ct);

        if (order is null) return (null, "Pedido no encontrado.");

        // 2. Validar estado
        if (order.StatusOrder != "Confirmado")
            return (null, "El pedido debe estar en estado 'Confirmado' para enviarse a producción.");

        // 3. Validar que no exista OP activa vinculada
        var existsActiveOp = await db.ProductionOrder
            .AnyAsync(po => po.IdSalesOrder == idSalesOrder
                         && po.StatusProductionOrder != "Anulado", ct);
        if (existsActiveOp)
            return (null, "El pedido ya tiene una orden de producción activa. Use el endpoint de completado.");

        var createdOrders = new List<CreatedProductionOrderInfo>();
        var opCount       = await db.ProductionOrder.CountAsync(po => po.StatusProductionOrder != "Borrador", ct);

        foreach (var line in order.SalesOrderLines)
        {
            // Cargar receta base del producto (activa)
            var baseRecipe = await db.ProductRecipe
                .Include(r => r.ProductRecipeLines)
                .FirstOrDefaultAsync(r => r.IdProductOutput == line.IdProduct && r.IsActive, ct);

            if (baseRecipe is null) continue;  // Sin receta activa → no genera OP

            // Acumular insumos base
            var inputAgg = baseRecipe.ProductRecipeLines
                .ToDictionary(
                    rl => rl.IdProductInput,
                    rl => rl.QuantityInput * line.QuantityBase / baseRecipe.QuantityOutput);

            // Agregar insumos de recetas de opciones
            foreach (var opt in line.SalesOrderLineOptions)
            {
                var optItem = opt.IdProductOptionItemNavigation;
                if (optItem.IdProductRecipe is null) continue;

                var optRecipe = await db.ProductRecipe
                    .Include(r => r.ProductRecipeLines)
                    .FirstOrDefaultAsync(r => r.IdProductRecipe == optItem.IdProductRecipe, ct);

                if (optRecipe is null) continue;

                foreach (var rl in optRecipe.ProductRecipeLines)
                {
                    var qty = rl.QuantityInput * opt.Quantity / optRecipe.QuantityOutput;
                    inputAgg[rl.IdProductInput] = inputAgg.TryGetValue(rl.IdProductInput, out var existing)
                        ? existing + qty
                        : qty;
                }
            }

            // Obtener unidad base del producto final
            var baseUnit = await db.ProductUnit
                .FirstOrDefaultAsync(pu => pu.IdProduct == line.IdProduct && pu.IsBase, ct);
            var idUnit = baseUnit?.IdProductUnit
                      ?? (await db.ProductUnit.FirstOrDefaultAsync(pu => pu.IdProduct == line.IdProduct, ct))?.IdProductUnit
                      ?? line.IdProductUnit;

            // Crear OP con los insumos combinados
            opCount++;
            var opNumber = $"OP-{DateTime.UtcNow:yyyy}-{opCount:D4}";
            var po = new ProductionOrder
            {
                IdFiscalPeriod        = order.IdFiscalPeriod,
                IdSalesOrder          = order.IdSalesOrder,
                IdWarehouse           = null,
                NumberProductionOrder = opNumber,
                DateOrder             = order.DateOrder,
                DateRequired          = order.DateDelivery,
                StatusProductionOrder = "Pendiente",
                DescriptionOrder      = $"Auto-generada desde pedido {order.NumberOrder}, línea #{line.IdSalesOrderLine}",
                CreatedAt             = DateTime.UtcNow
            };

            // Líneas de insumos (MP)
            foreach (var (idProduct, qty) in inputAgg)
            {
                var inpUnit = await db.ProductUnit
                    .FirstOrDefaultAsync(pu => pu.IdProduct == idProduct && pu.IsBase, ct)
                    ?? await db.ProductUnit.FirstOrDefaultAsync(pu => pu.IdProduct == idProduct, ct);

                po.ProductionOrderLines.Add(new ProductionOrderLine
                {
                    IdProduct        = idProduct,
                    IdProductUnit    = inpUnit?.IdProductUnit ?? 0,
                    IdSalesOrderLine = line.IdSalesOrderLine,
                    QuantityRequired = Math.Round(qty, 4),
                    QuantityProduced = 0m
                });
            }

            db.ProductionOrder.Add(po);

            // Crear Fulfillment entre línea del pedido y la OP
            db.SalesOrderLineFulfillment.Add(new SalesOrderLineFulfillment
            {
                IdSalesOrderLine  = line.IdSalesOrderLine,
                FulfillmentType   = "Produccion",
                IdProductionOrder = 0,  // se actualizará tras SaveChanges
                QuantityBase      = line.QuantityBase,
                CreatedAt         = DateTime.UtcNow
            });

            createdOrders.Add(new CreatedProductionOrderInfo(
                0,
                opNumber,
                line.IdSalesOrderLine,
                line.IdProductNavigation.NameProduct));
        }

        if (createdOrders.Count == 0)
            return (null, "Ninguna línea del pedido tiene una receta activa para generar órdenes de producción.");

        // 5. Cambiar estado
        order.StatusOrder = "EnProduccion";
        await db.SaveChangesAsync(ct);

        // 6. Actualizar IdProductionOrder en fulfillments (ahora tenemos los IDs)
        var fulfillments = await db.SalesOrderLineFulfillment
            .Where(f => f.IdSalesOrderLineNavigation.IdSalesOrder == idSalesOrder
                     && f.FulfillmentType == "Produccion"
                     && f.IdProductionOrder == 0)
            .ToListAsync(ct);

        var opsList = await db.ProductionOrder
            .Where(po => po.IdSalesOrder == idSalesOrder && po.StatusProductionOrder != "Anulado")
            .OrderBy(po => po.IdProductionOrder)
            .ToListAsync(ct);

        for (var i = 0; i < fulfillments.Count && i < opsList.Count; i++)
            fulfillments[i].IdProductionOrder = opsList[i].IdProductionOrder;

        await db.SaveChangesAsync(ct);

        // Completar respuesta con IDs reales
        var finalCreated = opsList.Select((po, i) => new CreatedProductionOrderInfo(
            po.IdProductionOrder,
            po.NumberProductionOrder,
            createdOrders[i].IdSalesOrderLine,
            createdOrders[i].ProductName)).ToList();

        return (new SendToProductionResponse(finalCreated), null);
    }

    // ── T17: complete ─────────────────────────────────────────────────────────

    public async Task<(bool Ok, string? Error)> CompleteOrderAsync(int idSalesOrder, CancellationToken ct = default)
    {
        var order = await db.SalesOrder.FindAsync([idSalesOrder], ct);
        if (order is null) return (false, "Pedido no encontrado.");
        if (order.StatusOrder != "EnProduccion")
            return (false, "El pedido debe estar en estado 'EnProduccion' para completarse.");

        // Verificar que todas las OPs vinculadas están completadas
        var ops = await db.ProductionOrder
            .Where(po => po.IdSalesOrder == idSalesOrder && po.StatusProductionOrder != "Anulado")
            .Select(po => new { po.IdProductionOrder, po.NumberProductionOrder, po.StatusProductionOrder })
            .ToListAsync(ct);

        var pending = ops.Where(po => po.StatusProductionOrder != "Completado").ToList();
        if (pending.Count > 0)
        {
            var detail = string.Join(", ", pending.Select(po => $"{po.NumberProductionOrder} ({po.StatusProductionOrder})"));
            return (false, $"Las siguientes órdenes de producción no están completadas: {detail}.");
        }

        order.StatusOrder = "Completado";
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    // ── T18: invoice ──────────────────────────────────────────────────────────

    public async Task<(GenerateInvoiceResponse? Result, string? Error)> GenerateInvoiceAsync(
        int idSalesOrder, CancellationToken ct = default)
    {
        var order = await db.SalesOrder
            .Include(so => so.SalesOrderLines)
                .ThenInclude(l => l.SalesOrderLineOptions)
                    .ThenInclude(o => o.IdProductOptionItemNavigation)
            .Include(so => so.SalesOrderLines)
                .ThenInclude(l => l.SalesOrderLineFulfillments)
            .FirstOrDefaultAsync(so => so.IdSalesOrder == idSalesOrder, ct);

        if (order is null) return (null, "Pedido no encontrado.");
        if (order.StatusOrder != "Completado")
            return (null, "El pedido debe estar en estado 'Completado' para facturarse.");

        // Verificar que no exista ya una factura
        var existingInvoice = await db.SalesInvoice
            .AsNoTracking()
            .FirstOrDefaultAsync(si => si.IdSalesOrder == idSalesOrder, ct);
        if (existingInvoice is not null)
            return (null, $"Ya existe la factura #{existingInvoice.NumberInvoice} para este pedido (IdSalesInvoice={existingInvoice.IdSalesInvoice}).");

        // Obtener el primer tipo de factura de venta disponible
        var invoiceType = await db.SalesInvoiceType
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
        if (invoiceType is null) return (null, "No se encontró ningún tipo de factura de venta configurado.");

        var invoice = new SalesInvoice
        {
            IdFiscalPeriod    = order.IdFiscalPeriod,
            IdCurrency        = order.IdCurrency,
            IdSalesInvoiceType = invoiceType.IdSalesInvoiceType,
            IdContact         = order.IdContact,
            IdSalesOrder      = order.IdSalesOrder,
            NumberInvoice     = "BORRADOR",
            DateInvoice       = DateOnly.FromDateTime(DateTime.UtcNow),
            StatusInvoice     = "Borrador",
            ExchangeRateValue = order.ExchangeRateValue,
            DescriptionInvoice = order.DescriptionOrder,
            CreatedAt         = DateTime.UtcNow
        };

        foreach (var line in order.SalesOrderLines)
        {
            // Buscar si hay fulfillment de producción con lote PT
            var fulfillment = line.SalesOrderLineFulfillments
                .FirstOrDefault(f => f.FulfillmentType == "Produccion" && f.IdInventoryLot.HasValue);

            var sil = new SalesInvoiceLine
            {
                IsNonProductLine = false,
                IdProduct        = line.IdProduct,
                IdUnit           = line.IdProductUnit,
                IdInventoryLot   = fulfillment?.IdInventoryLot,
                DescriptionLine  = line.DescriptionLine ?? string.Empty,
                Quantity         = line.Quantity,
                UnitPrice        = line.UnitPrice,
                TaxPercent       = line.TaxPercent,
                TotalLineAmount  = line.TotalLineAmount
            };

            // Copiar options
            foreach (var opt in line.SalesOrderLineOptions)
            {
                sil.SalesInvoiceLineOptions.Add(new SalesInvoiceLineOption
                {
                    IdProductOptionItem = opt.IdProductOptionItem,
                    Quantity            = opt.Quantity
                });
            }

            invoice.SalesInvoiceLines.Add(sil);
        }

        // Recalcular totales de la factura
        invoice.SubTotalAmount = invoice.SalesInvoiceLines.Sum(l => Math.Round(l.Quantity * l.UnitPrice, 2));
        invoice.TaxAmount      = invoice.SalesInvoiceLines.Sum(l => Math.Round(l.Quantity * l.UnitPrice * l.TaxPercent / 100m, 2));
        invoice.TotalAmount    = invoice.SubTotalAmount + invoice.TaxAmount;

        db.SalesInvoice.Add(invoice);
        await db.SaveChangesAsync(ct);

        return (new GenerateInvoiceResponse(invoice.IdSalesInvoice), null);
    }
}
