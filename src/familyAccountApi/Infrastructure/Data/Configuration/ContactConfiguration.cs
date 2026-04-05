using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo de contactos del sistema: clientes, proveedores u otras entidades externas. Cada contacto puede tener uno o más tipos asignados a través de la tabla contactContactType."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(c => c.IdContact);
        builder.Property(c => c.IdContact)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del contacto.");

        // ── Código ──────────────────────────────────────────
        builder.Property(c => c.CodeContact)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único de identificación del contacto. Usado internamente para referencias rápidas.");

        // ── Nombre ──────────────────────────────────────────
        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired()
            .IsUnicode()
            .HasComment("Nombre completo o razón social del contacto.");

        // ── Índices ─────────────────────────────────────────
        builder.HasIndex(c => c.CodeContact)
            .IsUnique()
            .HasDatabaseName("UQ_contact_codeContact");
        // ── Seed data ────────────────────────────────────────
        builder.HasData(
            new Contact { IdContact = 1, CodeContact = "SIN_PRO_CLI", Name = "Sin proveedor / Cliente" }
        );    }
}
