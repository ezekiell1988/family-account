namespace FamilyAccountApi.Features.Contacts.Dtos;

public sealed record ContactResponse(
    int    IdContact,
    string CodeContact,
    string Name);
