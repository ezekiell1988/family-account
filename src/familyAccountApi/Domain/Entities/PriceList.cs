namespace FamilyAccountApi.Domain.Entities;

public sealed class PriceList
{
    public int      IdPriceList   { get; set; }
    public string   NamePriceList { get; set; } = null!;
    public string?  Description   { get; set; }
    public DateOnly DateFrom      { get; set; }
    public DateOnly? DateTo       { get; set; }   // NULL = vigente indefinidamente
    public bool     IsActive      { get; set; }
    public DateTime CreatedAt     { get; set; }

    public ICollection<PriceListItem> PriceListItems { get; set; } = [];
    public ICollection<SalesOrder>    SalesOrders    { get; set; } = [];
}
