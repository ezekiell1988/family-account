namespace FamilyAccountApi.Features.ProductSKUs.Dtos;

public sealed record ProductSKUResponse(
    int     IdProductSKU,
    string  CodeProductSKU,
    string  NameProductSKU,
    string? BrandProductSKU,
    string? DescriptionProductSKU,
    string? NetContent);
