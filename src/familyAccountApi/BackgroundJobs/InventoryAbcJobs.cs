using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.BackgroundJobs;

/// <summary>
/// Job de Hangfire que recalcula la clasificación ABC de todos los productos
/// según el valor de ventas confirmadas en los últimos <see cref="WindowDays"/> días.
/// </summary>
/// <remarks>
/// Algoritmo Pareto:
///   A = productos cuya venta acumulada alcanza el 80% del total.
///   B = siguientes hasta el 95%.
///   C = el resto con ventas en el período.
///   NULL = sin ventas en el período (sin clasificación).
/// Se programa para ejecutarse semanalmente (domingos a las 02:00 AM).
/// </remarks>
public sealed class InventoryAbcJobs(AppDbContext db)
{
    private const int    WindowDays   = 90;
    private const double ThresholdA   = 0.80;
    private const double ThresholdAb  = 0.95;

    public async Task RecalculateAsync()
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-WindowDays));

        // ── 1. Sumar valor de ventas confirmadas por producto ──────────────
        var salesByProduct = await db.SalesInvoiceLine
            .AsNoTracking()
            .Where(l => l.IdProduct != null
                     && l.IdSalesInvoiceNavigation.StatusInvoice == "Confirmado"
                     && l.IdSalesInvoiceNavigation.DateInvoice    >= since)
            .GroupBy(l => l.IdProduct!.Value)
            .Select(g => new
            {
                IdProduct  = g.Key,
                TotalSales = g.Sum(l => l.TotalLineAmount)
            })
            .OrderByDescending(x => x.TotalSales)
            .ToListAsync();

        // ── 2. Limpiar clasificación de todos los productos ────────────────
        await db.Product.ExecuteUpdateAsync(
            s => s.SetProperty(p => p.ClassificationAbc, (string?)null));

        if (salesByProduct.Count == 0)
            return;

        // ── 3. Calcular umbrales acumulados ────────────────────────────────
        var grandTotal   = salesByProduct.Sum(x => (double)x.TotalSales);
        var cumulative   = 0.0;

        var classifications = new Dictionary<int, string>(salesByProduct.Count);

        foreach (var item in salesByProduct)
        {
            cumulative += (double)item.TotalSales;
            var ratio   = cumulative / grandTotal;

            var label = ratio <= ThresholdA  ? "A"
                      : ratio <= ThresholdAb ? "B"
                      :                        "C";

            classifications[item.IdProduct] = label;
        }

        // ── 4. Actualizar por lotes de 200 ────────────────────────────────
        const int batchSize = 200;

        var idsA = classifications.Where(kv => kv.Value == "A").Select(kv => kv.Key).ToList();
        var idsB = classifications.Where(kv => kv.Value == "B").Select(kv => kv.Key).ToList();
        var idsC = classifications.Where(kv => kv.Value == "C").Select(kv => kv.Key).ToList();

        await UpdateInBatchesAsync(idsA, "A", batchSize);
        await UpdateInBatchesAsync(idsB, "B", batchSize);
        await UpdateInBatchesAsync(idsC, "C", batchSize);
    }

    private async Task UpdateInBatchesAsync(List<int> ids, string label, int batchSize)
    {
        for (var i = 0; i < ids.Count; i += batchSize)
        {
            var batch = ids.Skip(i).Take(batchSize).ToList();
            await db.Product
                .Where(p => batch.Contains(p.IdProduct))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ClassificationAbc, label));
        }
    }
}
