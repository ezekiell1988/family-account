namespace FamilyAccountApi.Features.PriceLists.Dtos;

public sealed record PriceListResponse(
    int       IdPriceList,
    string    NamePriceList,
    string?   Description,
    DateOnly  DateFrom,
    DateOnly? DateTo,
    bool      IsActive,
    DateTime  CreatedAt,
    IReadOnlyList<PriceListItemResponse> Items);

public sealed record PriceListItemResponse(
    int      IdPriceListItem,
    int      IdProduct,
    string   NameProduct,
    int      IdProductUnit,
    string   NameUnit,
    decimal  UnitPrice,
    bool     IsActive);
