using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductionSnapshotLineConfiguration : IEntityTypeConfiguration<ProductionSnapshotLine>
{
    public void Configure(EntityTypeBuilder<ProductionSnapshotLine> builder)
    {
        builder.ToTable(t => t.HasComment("Línea del snapshot de producción. Una fila por insumo, con cantidad teórica calculada (según receta) y cantidad real usada. idProductRecipeLine es NULL cuando el operador agregó un insumo extra no previsto en la receta."));

        builder.HasKey(psl => psl.IdProductionSnapshotLine);
        builder.Property(psl => psl.IdProductionSnapshotLine)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la línea.");

        builder.Property(psl => psl.IdProductionSnapshot)
            .IsRequired()
            .HasComment("FK al snapshot de producción cabecera.");

        builder.Property(psl => psl.IdProductRecipeLine)
            .HasComment("FK a la línea de receta de origen. NULL si es un insumo extra no previsto en la receta.");

        builder.Property(psl => psl.IdProductInput)
            .IsRequired()
            .HasComment("Snapshot del producto insumo, desacoplado de la línea de receta para sobrevivir cambios futuros en la misma.");

        builder.Property(psl => psl.QuantityCalculated)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasComment("Cantidad teórica: ProductRecipeLine.QuantityInput × (QuantityReal / QuantityCalculated de la cabecera). 0 para insumos extra.");

        builder.Property(psl => psl.QuantityReal)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasComment("Cantidad real usada por el operador en esta corrida.");

        builder.Property(psl => psl.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Orden de visualización, copiado de ProductRecipeLine.SortOrder.");

        // ── Relaciones ──────────────────────────────────────
        builder.HasOne(psl => psl.IdProductionSnapshotNavigation)
            .WithMany(ps => ps.ProductionSnapshotLines)
            .HasForeignKey(psl => psl.IdProductionSnapshot)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(psl => psl.IdProductRecipeLineNavigation)
            .WithMany()
            .HasForeignKey(psl => psl.IdProductRecipeLine)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(psl => psl.IdProductInputNavigation)
            .WithMany()
            .HasForeignKey(psl => psl.IdProductInput)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Índices ─────────────────────────────────────────
        builder.HasIndex(psl => psl.IdProductionSnapshot)
            .HasDatabaseName("IX_productionSnapshotLine_idProductionSnapshot");

        builder.HasIndex(psl => psl.IdProductRecipeLine)
            .HasFilter("[idProductRecipeLine] IS NOT NULL")
            .HasDatabaseName("IX_productionSnapshotLine_idProductRecipeLine");
    }
}
