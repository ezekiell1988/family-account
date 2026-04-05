namespace FamilyAccountApi.Domain.Entities;

public sealed class SalesOrderAdvance
{
    public int      IdSalesOrderAdvance  { get; set; }
    public int      IdSalesOrder         { get; set; }
    public int      IdAccountingEntry    { get; set; }   // Asiento contable del anticipo
    public int?     IdProductionOrder    { get; set; }   // Contexto informativo: ¿en qué momento de la orden se recibió?
    public decimal  Amount               { get; set; }
    public DateOnly DateAdvance          { get; set; }
    public string?  DescriptionAdvance   { get; set; }
    public DateTime CreatedAt            { get; set; }

    public SalesOrder       IdSalesOrderNavigation       { get; set; } = null!;
    public AccountingEntry  IdAccountingEntryNavigation  { get; set; } = null!;
    public ProductionOrder? IdProductionOrderNavigation  { get; set; }
}
