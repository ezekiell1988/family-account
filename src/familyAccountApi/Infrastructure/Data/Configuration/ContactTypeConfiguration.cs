using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ContactTypeConfiguration : IEntityTypeConfiguration<ContactType>
{
    public void Configure(EntityTypeBuilder<ContactType> builder)
    {
        // ── PK ──────────────────────────────────────────────
        builder.HasKey(ct => ct.IdContactType);
        builder.Property(ct => ct.IdContactType).ValueGeneratedOnAdd();

        // ── Código ──────────────────────────────────────────
        builder.Property(ct => ct.CodeContactType)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);

        // ── Nombre ──────────────────────────────────────────
        builder.Property(ct => ct.Name)
            .HasMaxLength(200)
            .IsRequired()
            .IsUnicode();

        // ── Índices ─────────────────────────────────────────
        builder.HasIndex(ct => ct.CodeContactType)
            .IsUnique()
            .HasDatabaseName("UQ_contactType_codeContactType");

        // ── Seed data ────────────────────────────────────────
        builder.HasData(
            new ContactType { IdContactType = 1, CodeContactType = "CLI", Name = "Cliente" },
            new ContactType { IdContactType = 2, CodeContactType = "PRO", Name = "Proveedor" }
        );
    }
}
