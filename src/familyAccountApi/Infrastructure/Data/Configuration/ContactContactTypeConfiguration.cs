using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ContactContactTypeConfiguration : IEntityTypeConfiguration<ContactContactType>
{
    public void Configure(EntityTypeBuilder<ContactContactType> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla de asociación muchos-a-muchos entre contactos y tipos de contacto. Permite que un mismo contacto sea clasificado como Cliente, Proveedor u otros tipos simultáneamente. No se permite la misma combinación contacto-tipo dos veces."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(cct => cct.IdContactContactType);
        builder.Property(cct => cct.IdContactContactType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la asociación contacto-tipo.");

        builder.Property(cct => cct.IdContact)
            .HasComment("FK al contacto.");

        builder.Property(cct => cct.IdContactType)
            .HasComment("FK al tipo de contacto.");

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
