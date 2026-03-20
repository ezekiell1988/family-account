namespace FamilyAccountApi.Features.Products.Dtos;

public sealed record ProductSKUSummary(
    int     IdProductSKU,
    string  CodeProductSKU,
    string  NameProductSKU,
    string? BrandProductSKU,
    string? NetContent);

public sealed record ProductResponse(
    int    IdProduct,
    string CodeProduct,
    string NameProduct,
    IReadOnlyList<ProductSKUSummary> SKUs);
