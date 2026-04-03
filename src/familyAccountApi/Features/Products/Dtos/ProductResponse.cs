namespace FamilyAccountApi.Features.Products.Dtos;

public sealed record ProductResponse(
    int      IdProduct,
    string   CodeProduct,
    string   NameProduct,
    int      IdProductType,
    string   NameProductType,
    int      IdUnit,
    string   CodeUnit,
    int?     IdProductParent,
    decimal  AverageCost);
