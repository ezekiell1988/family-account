namespace FamilyAccountApi.Features.UnitsOfMeasure.Dtos;

public sealed record UnitOfMeasureResponse(
    int    IdUnit,
    string CodeUnit,
    string NameUnit,
    int    IdUnitType,
    string NameUnitType);
