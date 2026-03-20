namespace FamilyAccountApi.Domain.Entities;

public sealed class Contact
{
    public int IdContact { get; set; }
    public string CodeContact { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<ContactContactType> ContactContactTypes { get; set; } = [];
}
