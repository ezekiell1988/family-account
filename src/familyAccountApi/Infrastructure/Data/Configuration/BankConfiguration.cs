using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.ToTable(t =>
            t.HasComment("Catálogo de entidades bancarias. Representa los bancos o instituciones financieras."));

        builder.HasKey(b => b.IdBank);
        builder.Property(b => b.IdBank)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del banco.");

        builder.Property(b => b.CodeBank)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único de la entidad bancaria. Ejemplo: BCR, BN, BAC, COOPEALIANZA.");

        builder.Property(b => b.NameBank)
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("Nombre completo de la entidad bancaria.");

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si el banco está activo y disponible para asociar cuentas bancarias.");

        builder.HasIndex(b => b.CodeBank)
            .IsUnique()
            .HasDatabaseName("UQ_bank_codeBank");

        // ── Seed ──────────────────────────────────────────────────────────────
        builder.HasData(
            new Bank { IdBank = 1, CodeBank = "BCR",          NameBank = "Banco de Costa Rica",                    IsActive = true },
            new Bank { IdBank = 2, CodeBank = "BAC",          NameBank = "BAC Credomatic",                         IsActive = true },
            new Bank { IdBank = 3, CodeBank = "BN",           NameBank = "Banco Nacional de Costa Rica",           IsActive = true },
            new Bank { IdBank = 4, CodeBank = "COOPEALIANZA", NameBank = "Coopealianza",                           IsActive = true },
            new Bank { IdBank = 5, CodeBank = "DAVIVIENDA",   NameBank = "Davivienda",                             IsActive = true },
            new Bank { IdBank = 6, CodeBank = "BPOPULAR",     NameBank = "Banco Popular y de Desarrollo Comunal", IsActive = true }
        );
    }
}
