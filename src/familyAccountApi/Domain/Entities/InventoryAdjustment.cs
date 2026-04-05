namespace FamilyAccountApi.Domain.Entities;

public sealed class InventoryAdjustment
{
    public int      IdInventoryAdjustment     { get; set; }
    public int      IdFiscalPeriod            { get; set; }
    public int      IdInventoryAdjustmentType { get; set; }
    public int      IdCurrency                { get; set; }
    public decimal  ExchangeRateValue         { get; set; }
    public string   NumberAdjustment          { get; set; } = null!;
    public DateOnly DateAdjustment            { get; set; }
    public string?  DescriptionAdjustment     { get; set; }
    public string   StatusAdjustment          { get; set; } = null!;
    public DateTime CreatedAt                 { get; set; }

    public FiscalPeriod              IdFiscalPeriodNavigation            { get; set; } = null!;
    public InventoryAdjustmentType   IdInventoryAdjustmentTypeNavigation { get; set; } = null!;
    public Currency                  IdCurrencyNavigation                { get; set; } = null!;
    public ICollection<InventoryAdjustmentLine>  InventoryAdjustmentLines   { get; set; } = [];
    public ICollection<InventoryAdjustmentEntry> InventoryAdjustmentEntries { get; set; } = [];
}
