using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable(t =>
            t.HasComment("Empresas registradas en el sistema."));

        builder.HasKey(c => c.IdCompany);
        builder.Property(c => c.IdCompany)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la empresa.");

        builder.Property(c => c.CodeCompany)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único de la empresa.");

        builder.Property(c => c.NameCompany)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre completo de la empresa.");

        builder.HasIndex(c => c.CodeCompany)
            .IsUnique()
            .HasDatabaseName("UQ_company_codeCompany");

        builder.HasData(
            new Company { IdCompany = 1, CodeCompany = "FBS",   NameCompany = "Familia Baltodano Soto" },
            new Company { IdCompany = 2, CodeCompany = "CDSRL", NameCompany = "Corporacion los diablitos SRL" }
        );
    }
}
