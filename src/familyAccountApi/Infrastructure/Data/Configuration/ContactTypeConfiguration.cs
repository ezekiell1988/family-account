using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ContactTypeConfiguration : IEntityTypeConfiguration<ContactType>
{
    public void Configure(EntityTypeBuilder<ContactType> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo de tipos de contacto. Permite clasificar un contacto con una o más categorías (ej: Cliente, Proveedor). Los valores se asignan a contactos a través de la tabla contactContactType."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(ct => ct.IdContactType);
        builder.Property(ct => ct.IdContactType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del tipo de contacto.");

        // ── Código ──────────────────────────────────────────
        builder.Property(ct => ct.CodeContactType)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código abreviado único del tipo de contacto. Ej: 'CLI' (Cliente), 'PRO' (Proveedor).");

        // ── Nombre ──────────────────────────────────────────
        builder.Property(ct => ct.Name)
            .HasMaxLength(200)
            .IsRequired()
            .IsUnicode()
            .HasComment("Nombre descriptivo del tipo de contacto. Ej: 'Cliente', 'Proveedor'.");

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
