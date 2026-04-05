namespace FamilyAccountApi.Domain.Entities;

/// <summary>
/// Detalle de movimiento de inventario generado por explosión BOM (receta) o
/// por slot de combo al confirmar una SalesInvoiceLine. Una línea de factura
/// puede originar N registros: uno por insumo de receta o por producto de slot.
/// </summary>
public sealed class SalesInvoiceLineBomDetail
{
    public int     IdSalesInvoiceLineBomDetail { get; set; }
    public int     IdSalesInvoiceLine          { get; set; }

    /// <summary>
    /// FK nullable al slot del combo del que proviene este movimiento.
    /// NULL si la línea no es un combo (solo tiene explosión de receta).
    /// </summary>
    public int?    IdProductComboSlot          { get; set; }

    /// <summary>
    /// FK nullable a la línea de receta que originó este movimiento.
    /// NULL cuando el producto es de reventa (slot directo sin receta)
    /// o cuando es un insumo extra no previsto en la receta.
    /// </summary>
    public int?    IdProductRecipeLine         { get; set; }

    /// <summary>Insumo o producto de slot descontado.</summary>
    public int     IdProduct                   { get; set; }

    /// <summary>Lote específico del que se descontó el stock.</summary>
    public int     IdInventoryLot              { get; set; }

    /// <summary>Cantidad descontada en unidad base del insumo/producto.</summary>
    public decimal QuantityConsumed            { get; set; }

    /// <summary>Snapshot del costo unitario del lote al momento de confirmar.</summary>
    public decimal UnitCost                    { get; set; }

    // ── Navegaciones ──────────────────────────────────────────────────────────
    public SalesInvoiceLine    IdSalesInvoiceLineNavigation    { get; set; } = null!;
    public ProductComboSlot?   IdProductComboSlotNavigation    { get; set; }
    public ProductRecipeLine?  IdProductRecipeLineNavigation   { get; set; }
    public Product             IdProductNavigation             { get; set; } = null!;
    public InventoryLot        IdInventoryLotNavigation        { get; set; } = null!;
}
