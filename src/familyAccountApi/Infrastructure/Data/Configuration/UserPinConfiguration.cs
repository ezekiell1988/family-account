using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class UserPinConfiguration : IEntityTypeConfiguration<UserPin>
{
    public void Configure(EntityTypeBuilder<UserPin> builder)
    {
        builder.ToTable(t => t.HasComment("PINs temporales de 5 dígitos usados para autenticación de dos factores. Se generan por solicitud y se envían al correo del usuario. Un PIN no puede repetirse para el mismo usuario hasta que expire o sea usado."));

        builder.HasKey(up => up.IdUserPin);
        builder.Property(up => up.IdUserPin)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del registro de PIN.");

        builder.Property(up => up.IdUser)
            .IsRequired()
            .HasComment("FK al usuario propietario del PIN.");

        builder.Property(up => up.CreateAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired()
            .HasComment("Fecha y hora en que se generó el PIN. Se usa para validar su vigencia.");

        builder.Property(up => up.Pin)
            .HasMaxLength(5)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("PIN numérico de 5 dígitos generado aleatoriamente y enviado al usuario por correo.");

        // Un mismo PIN no puede repetirse para el mismo usuario
        builder.HasIndex(up => new { up.IdUser, up.Pin })
            .IsUnique()
            .HasDatabaseName("UQ_userPin_idUser_pin");

        builder.HasOne(up => up.User)
            .WithMany(u => u.UserPins)
            .HasForeignKey(up => up.IdUser)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
