using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ContactContactTypeConfiguration : IEntityTypeConfiguration<ContactContactType>
{
    public void Configure(EntityTypeBuilder<ContactContactType> builder)
    {
        // ── PK ──────────────────────────────────────────────
        builder.HasKey(cct => cct.IdContactContactType);
        builder.Property(cct => cct.IdContactContactType).ValueGeneratedOnAdd();

        // ── Índice único compuesto ───────────────────────────
        builder.HasIndex(cct => new { cct.IdContact, cct.IdContactType })
            .IsUnique()
            .HasDatabaseName("UQ_contactContactType_idContact_idContactType");

        // ── FK → Contact ─────────────────────────────────────
        builder.HasOne(cct => cct.Contact)
            .WithMany(c => c.ContactContactTypes)
            .HasForeignKey(cct => cct.IdContact)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK → ContactType ─────────────────────────────────
        builder.HasOne(cct => cct.ContactType)
            .WithMany(ct => ct.ContactContactTypes)
            .HasForeignKey(cct => cct.IdContactType)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
