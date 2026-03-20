namespace FamilyAccountApi.Domain.Entities;

public sealed class ContactType
{
    public int IdContactType { get; set; }
    public string CodeContactType { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<ContactContactType> ContactContactTypes { get; set; } = [];
}
