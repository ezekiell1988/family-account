using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.IdRole);
        builder.Property(r => r.IdRole).ValueGeneratedOnAdd();

        builder.Property(r => r.CreateAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();

        builder.Property(r => r.NameRole)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);

        builder.Property(r => r.DescriptionRole)
            .HasMaxLength(200)
            .IsUnicode(false);

        builder.HasIndex(r => r.NameRole)
            .IsUnique()
            .HasDatabaseName("UQ_role_nameRole");

        builder.HasData(
            new Role
            {
                IdRole          = 1,
                CreateAt        = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Unspecified),
                NameRole        = "Developer",
                DescriptionRole = "Acceso total al sistema"
            },
            new Role
            {
                IdRole          = 2,
                CreateAt        = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Unspecified),
                NameRole        = "Admin",
                DescriptionRole = "Administrador con acceso amplio"
            },
            new Role
            {
                IdRole          = 3,
                CreateAt        = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Unspecified),
                NameRole        = "User",
                DescriptionRole = "Usuario estándar"
            });
    }
}
