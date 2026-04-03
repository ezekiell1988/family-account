namespace FamilyAccountApi.Domain.Entities;

public sealed class UnitOfMeasure
{
    public int    IdUnit    { get; set; }
    public string CodeUnit  { get; set; } = null!;
    public string NameUnit  { get; set; } = null!;
    public string TypeUnit  { get; set; } = null!;

    public ICollection<Product>     Products     { get; set; } = [];
    public ICollection<ProductUnit> ProductUnits { get; set; } = [];
}
