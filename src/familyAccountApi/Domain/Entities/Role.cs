namespace FamilyAccountApi.Domain.Entities;

public sealed class Role
{
    public int IdRole { get; set; }
    public DateTime CreateAt { get; set; }
    public string NameRole { get; set; } = null!;
    public string? DescriptionRole { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
