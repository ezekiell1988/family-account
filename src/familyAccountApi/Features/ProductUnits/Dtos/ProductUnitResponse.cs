namespace FamilyAccountApi.Features.ProductUnits.Dtos;

public sealed record ProductUnitResponse(
    int      IdProductUnit,
    int      IdProduct,
    int      IdUnit,
    string   CodeUnit,
    decimal  ConversionFactor,
    bool     IsBase,
    bool     UsedForPurchase,
    bool     UsedForSale,
    string?  CodeBarcode,
    string?  NamePresentation,
    string?  BrandPresentation,
    decimal  SalePrice);
