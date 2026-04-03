namespace FamilyAccountApi.Features.UnitsOfMeasure.Dtos;

public sealed record UnitOfMeasureResponse(
    int    IdUnit,
    string CodeUnit,
    string NameUnit,
    string TypeUnit);
