namespace FamilyAccountApi.Domain.Entities;

public sealed class UserPin
{
    public int IdUserPin { get; set; }
    public int IdUser { get; set; }
    public string Pin { get; set; } = null!;

    public User User { get; set; } = null!;
}
