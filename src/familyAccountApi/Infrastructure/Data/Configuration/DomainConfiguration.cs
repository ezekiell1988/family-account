using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class DomainConfiguration : IEntityTypeConfiguration<CompanyDomain>
{
    public void Configure(EntityTypeBuilder<CompanyDomain> builder)
    {
        // ── PK ──────────────────────────────────────────────
        builder.HasKey(d => d.IdDomain);
        builder.Property(d => d.IdDomain).ValueGeneratedOnAdd();

        // ── Columna domainUrl ────────────────────────────────
        builder.Property(d => d.DomainUrl)
            .HasMaxLength(200)
            .IsRequired()
            .IsUnicode(false);

        // ── FK → Company ─────────────────────────────────────
        builder.HasOne(d => d.Company)
            .WithMany(c => c.Domains)
            .HasForeignKey(d => d.IdCompany)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Índices ──────────────────────────────────────────
        builder.HasIndex(d => d.DomainUrl)
            .IsUnique()
            .HasDatabaseName("UQ_domain_domain");

        // ── Seed data ────────────────────────────────────────
        builder.HasData(
            new CompanyDomain { IdDomain = 1, DomainUrl = "localhost:8000",      IdCompany = 1 },
            new CompanyDomain { IdDomain = 2, DomainUrl = "localhost:8001",      IdCompany = 1 },
            new CompanyDomain { IdDomain = 3, DomainUrl = "diablitos.ezekl.com", IdCompany = 2 }
        );
    }
}
