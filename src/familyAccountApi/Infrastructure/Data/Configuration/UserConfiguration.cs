using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.IdUser);
        builder.Property(u => u.IdUser).ValueGeneratedOnAdd();

        builder.Property(u => u.CodeUser)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode();

        builder.Property(u => u.NameUser)
            .HasMaxLength(150)
            .IsRequired()
            .IsUnicode();

        builder.Property(u => u.PhoneUser)
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(u => u.EmailUser)
            .HasMaxLength(200)
            .IsRequired()
            .IsUnicode(false);

        builder.HasIndex(u => u.CodeUser)
            .IsUnique()
            .HasDatabaseName("UQ_user_codeUser");

        builder.HasData(new User
        {
            IdUser    = 1,
            CodeUser  = "S",
            NameUser  = "Ezequiel Baltodano Cubillo",
            PhoneUser = "50683681485",
            EmailUser = "ezekiell1988@hotmail.com"
        });
    }
}
