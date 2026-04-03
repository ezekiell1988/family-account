namespace FamilyAccountApi.Domain.Entities;

public sealed class ProductUnit
{
    public int      IdProductUnit      { get; set; }
    public int      IdProduct          { get; set; }
    public int      IdUnit             { get; set; }
    public decimal  ConversionFactor   { get; set; }
    public bool     IsBase             { get; set; }
    public bool     UsedForPurchase    { get; set; }
    public bool     UsedForSale        { get; set; }
    public string?  CodeBarcode        { get; set; }
    public string?  NamePresentation   { get; set; }
    public string?  BrandPresentation  { get; set; }

    public Product       Product       { get; set; } = null!;
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
}
