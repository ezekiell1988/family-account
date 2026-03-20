namespace FamilyAccountApi.Domain.Entities;

public sealed class ContactContactType
{
    public int IdContactContactType { get; set; }
    public int IdContact { get; set; }
    public int IdContactType { get; set; }

    public Contact Contact { get; set; } = null!;
    public ContactType ContactType { get; set; } = null!;
}
