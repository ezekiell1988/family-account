using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        // ── PK ──────────────────────────────────────────────
        builder.HasKey(c => c.IdContact);
        builder.Property(c => c.IdContact).ValueGeneratedOnAdd();

        // ── Código ──────────────────────────────────────────
        builder.Property(c => c.CodeContact)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);

        // ── Nombre ──────────────────────────────────────────
        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired()
            .IsUnicode();

        // ── Índices ─────────────────────────────────────────
        builder.HasIndex(c => c.CodeContact)
            .IsUnique()
            .HasDatabaseName("UQ_contact_codeContact");
    }
}
