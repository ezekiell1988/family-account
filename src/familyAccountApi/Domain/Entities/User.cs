namespace FamilyAccountApi.Domain.Entities;

public sealed class User
{
    public int IdUser { get; set; }
    public string CodeUser { get; set; } = null!;
    public string NameUser { get; set; } = null!;
    public string? PhoneUser { get; set; }
    public string EmailUser { get; set; } = null!;

    public ICollection<UserPin> UserPins { get; set; } = [];
}
