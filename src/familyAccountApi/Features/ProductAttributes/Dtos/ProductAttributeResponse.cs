namespace FamilyAccountApi.Features.ProductAttributes.Dtos;

public sealed record AttributeValueResponse(
    int    IdAttributeValue,
    string NameValue,
    int    SortOrder);

public sealed record ProductAttributeResponse(
    int    IdProductAttribute,
    string NameAttribute,
    int    SortOrder,
    IReadOnlyList<AttributeValueResponse> Values);
