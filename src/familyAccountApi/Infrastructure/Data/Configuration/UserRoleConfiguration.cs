using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(ur => ur.IdUserRole);
        builder.Property(ur => ur.IdUserRole).ValueGeneratedOnAdd();

        builder.Property(ur => ur.CreateAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();

        // Un usuario no puede tener asignado el mismo rol dos veces
        builder.HasIndex(ur => new { ur.IdUser, ur.IdRole })
            .IsUnique()
            .HasDatabaseName("UQ_userRole_idUser_idRole");

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.IdUser)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.IdRole)
            .OnDelete(DeleteBehavior.Cascade);

        // El usuario seed (idUser = 1) obtiene rol Developer (idRole = 1)
        builder.HasData(new UserRole
        {
            IdUserRole = 1,
            IdUser     = 1,
            IdRole     = 1,
            CreateAt   = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Unspecified)
        });
    }
}
