namespace FamilyAccountApi.Domain.Entities;

public sealed class UserRole
{
    public int IdUserRole { get; set; }
    public int IdUser { get; set; }
    public int IdRole { get; set; }
    public DateTime CreateAt { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
