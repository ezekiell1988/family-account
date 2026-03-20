using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla de asociación muchos-a-muchos entre usuarios y roles. Un usuario puede tener múltiples roles y un rol puede asignarse a múltiples usuarios. No se permite asignar el mismo rol dos veces al mismo usuario."));

        builder.HasKey(ur => ur.IdUserRole);
        builder.Property(ur => ur.IdUserRole)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la asignación usuario-rol.");

        builder.Property(ur => ur.CreateAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired()
            .HasComment("Fecha y hora en que se asignó el rol al usuario.");

        builder.Property(ur => ur.IdUser)
            .HasComment("FK al usuario al que se le asigna el rol.");

        builder.Property(ur => ur.IdRole)
            .HasComment("FK al rol que se le asigna al usuario.");

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

        // El usuario seed (idUser = 1) obtiene los tres roles por defecto
        builder.HasData(
            new UserRole { IdUserRole = 1, IdUser = 1, IdRole = 1, CreateAt = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Unspecified) },
            new UserRole { IdUserRole = 2, IdUser = 1, IdRole = 2, CreateAt = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Unspecified) },
            new UserRole { IdUserRole = 3, IdUser = 1, IdRole = 3, CreateAt = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Unspecified) }
        );
    }
}
