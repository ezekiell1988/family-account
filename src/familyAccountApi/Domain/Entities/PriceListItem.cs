namespace FamilyAccountApi.Domain.Entities;

public sealed class PriceListItem
{
    public int      IdPriceListItem { get; set; }
    public int      IdPriceList     { get; set; }
    public int      IdProduct       { get; set; }
    public int      IdProductUnit   { get; set; }   // Unidad de venta (presentación)
    public decimal  UnitPrice       { get; set; }
    public bool     IsActive        { get; set; }
    public DateTime CreatedAt       { get; set; }

    public PriceList    IdPriceListNavigation    { get; set; } = null!;
    public Product      IdProductNavigation      { get; set; } = null!;
    public ProductUnit  IdProductUnitNavigation  { get; set; } = null!;
}
