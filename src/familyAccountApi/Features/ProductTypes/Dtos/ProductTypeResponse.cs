namespace FamilyAccountApi.Features.ProductTypes.Dtos;

public sealed record ProductTypeResponse(
    int     IdProductType,
    string  NameProductType,
    string? DescriptionProductType);
