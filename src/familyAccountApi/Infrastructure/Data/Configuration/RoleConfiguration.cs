using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(t => t.HasComment("Roles de acceso del sistema. Define los niveles de autorización: Developer (acceso total), Admin (acceso amplio), User (acceso básico). Los roles se asignan a usuarios a través de la tabla userRole."));

        builder.HasKey(r => r.IdRole);
        builder.Property(r => r.IdRole)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del rol.");

        builder.Property(r => r.CreateAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired()
            .HasComment("Fecha y hora de creación del rol.");

        builder.Property(r => r.NameRole)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Nombre único del rol. Valores del sistema: Developer, Admin, User.");

        builder.Property(r => r.DescriptionRole)
            .HasMaxLength(200)
            .IsUnicode(false)
            .HasComment("Descripción del nivel de acceso y permisos que otorga el rol.");

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
