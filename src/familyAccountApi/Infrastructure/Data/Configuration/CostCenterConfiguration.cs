using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.ToTable(t =>
            t.HasComment("Centros de costo para clasificar los asientos contables por área, proyecto o departamento."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(cc => cc.IdCostCenter);
        builder.Property(cc => cc.IdCostCenter)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del centro de costo.");

        // ── Campos ─────────────────────────────────────────
        builder.Property(cc => cc.CodeCostCenter)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único del centro de costo. Ejemplo: ADM, VTA, PROD.");

        builder.Property(cc => cc.NameCostCenter)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre descriptivo del centro de costo. Ejemplo: Administración, Ventas.");

        builder.Property(cc => cc.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si el centro de costo está activo y disponible para su uso en asientos contables.");

        // ── Índice único ────────────────────────────────────
        builder.HasIndex(cc => cc.CodeCostCenter)
            .IsUnique()
            .HasDatabaseName("UQ_costCenter_codeCostCenter");
        // ── Seed ────────────────────────────────────────────────────────────
        builder.HasData(
            new CostCenter { IdCostCenter = 1, CodeCostCenter = "FAM-KYE",  NameCostCenter = "Familia Baltodano Soto (K & E)",    IsActive = true },
            new CostCenter { IdCostCenter = 2, CodeCostCenter = "FAM-PAPA", NameCostCenter = "Familia Baltodano Cubillo (Papás)", IsActive = true },
            new CostCenter { IdCostCenter = 3, CodeCostCenter = "OTROS",    NameCostCenter = "Otros",                             IsActive = true }
        );    }
}
