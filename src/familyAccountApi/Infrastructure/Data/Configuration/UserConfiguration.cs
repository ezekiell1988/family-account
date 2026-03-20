using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(t => t.HasComment("Usuarios del sistema que pueden autenticarse con JWT. Cada usuario puede tener uno o más roles (Developer, Admin, User)."));

        builder.HasKey(u => u.IdUser);
        builder.Property(u => u.IdUser)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del usuario.");

        builder.Property(u => u.CreateAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired()
            .HasComment("Fecha y hora de creación del registro. Se asigna automáticamente a la fecha actual del servidor.");

        builder.Property(u => u.CodeUser)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode()
            .HasComment("Código único de identificación del usuario. Usado como nombre de usuario en el login.");

        builder.Property(u => u.NameUser)
            .HasMaxLength(150)
            .IsRequired()
            .IsUnicode()
            .HasComment("Nombre completo del usuario.");

        builder.Property(u => u.PhoneUser)
            .HasMaxLength(20)
            .IsUnicode(false)
            .HasComment("Número de teléfono del usuario (opcional). Formato libre, ej: '50683681485'.");

        builder.Property(u => u.EmailUser)
            .HasMaxLength(200)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Correo electrónico del usuario. Usado para notificaciones y envío de PIN.");

        builder.HasIndex(u => u.CodeUser)
            .IsUnique()
            .HasDatabaseName("UQ_user_codeUser");

        builder.HasData(new User
        {
            IdUser    = 1,
            CreateAt  = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Unspecified),
            CodeUser  = "S",
            NameUser  = "Ezequiel Baltodano Cubillo",
            PhoneUser = "50683681485",
            EmailUser = "ezekiell1988@hotmail.com"
        });
    }
}
