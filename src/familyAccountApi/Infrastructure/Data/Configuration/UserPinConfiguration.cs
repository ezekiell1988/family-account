using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class UserPinConfiguration : IEntityTypeConfiguration<UserPin>
{
    public void Configure(EntityTypeBuilder<UserPin> builder)
    {
        builder.HasKey(up => up.IdUserPin);
        builder.Property(up => up.IdUserPin).ValueGeneratedOnAdd();

        builder.Property(up => up.IdUser).IsRequired();

        builder.Property(up => up.CreateAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("GETDATE()")
            .IsRequired();

        builder.Property(up => up.Pin)
            .HasMaxLength(5)
            .IsRequired()
            .IsUnicode(false);

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
