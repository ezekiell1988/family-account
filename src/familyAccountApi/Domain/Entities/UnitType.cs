namespace FamilyAccountApi.Domain.Entities;

public sealed class UnitType
{
    public int    IdUnitType    { get; set; }
    public string NameUnitType  { get; set; } = null!;

    public ICollection<UnitOfMeasure> UnitsOfMeasure { get; set; } = [];
}
