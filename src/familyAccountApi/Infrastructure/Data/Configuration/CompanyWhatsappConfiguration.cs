using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class CompanyWhatsappConfiguration : IEntityTypeConfiguration<CompanyWhatsapp>
{
    public void Configure(EntityTypeBuilder<CompanyWhatsapp> builder)
    {
        // ── PK ──────────────────────────────────────────────
        builder.HasKey(w => w.IdCompanyWhatsapp);
        builder.Property(w => w.IdCompanyWhatsapp).ValueGeneratedOnAdd();

        // ── Número de teléfono ───────────────────────────────
        builder.Property(w => w.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false);

        // ── IDs de Meta ─────────────────────────────────────
        builder.Property(w => w.PhoneNumberId)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);

        builder.Property(w => w.WabaId)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);

        // ── Tokens ──────────────────────────────────────────
        builder.Property(w => w.AccessToken)
            .HasMaxLength(512)
            .IsRequired()
            .IsUnicode(false);

        builder.Property(w => w.WebhookVerifyToken)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(false);

        builder.Property(w => w.ApiVersion)
            .HasMaxLength(10)
            .IsRequired()
            .IsUnicode(false);

        // ── FK → Company ─────────────────────────────────────
        builder.HasOne(w => w.Company)
            .WithMany(c => c.Whatsapps)
            .HasForeignKey(w => w.IdCompany)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Índice único: una config activa por empresa ──────
        builder.HasIndex(w => w.IdCompany)
            .IsUnique()
            .HasDatabaseName("UQ_companyWhatsapp_idCompany");

        // ── Seed data ────────────────────────────────────────
        builder.HasData(
            new CompanyWhatsapp
            {
                IdCompanyWhatsapp  = 1,
                IdCompany          = 2,
                PhoneNumber        = "+15550636204",
                PhoneNumberId      = "102981099397560",
                WabaId             = "110007718685670",
                AccessToken        = "EAAvhrvgZBWCQBOZBxsB7YHZCNISSIugqpkPDDG6UZCgQv0AFqHFE9BtT7tXYlTygFDfJ3BhlCFAAPD6Pu7rVsI0orXxhxsMvDqsCF3alYbU9T8CYQQCzViv6Rck94yHkYr7ueiJLL4M4XLax46rLyULdZBwESpW5TvKoS6UDnS9byoZA73gM8BAHDgd3KZCcpPo",
                WebhookVerifyToken = "mi_token_secreto_whatsapp_2024",
                ApiVersion         = "v24.0",
                IsActive           = true
            }
        );
    }
}
