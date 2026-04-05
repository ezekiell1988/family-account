using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductOptionItemAvailabilityConfiguration : IEntityTypeConfiguration<ProductOptionItemAvailability>
{
    public void Configure(EntityTypeBuilder<ProductOptionItemAvailability> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Reglas de disponibilidad condicional entre items de opción. " +
            "El item restringido (idRestrictedItem) solo está disponible cuando al menos uno " +
            "de sus ítems habilitadores (idEnablingItem) está seleccionado en el pedido."));

        builder.HasKey(r => r.IdProductOptionItemAvailability);
        builder.Property(r => r.IdProductOptionItemAvailability)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(r => r.IdRestrictedItem)
            .IsRequired()
            .HasComment("FK al ítem restringido.");

        builder.Property(r => r.IdEnablingItem)
            .IsRequired()
            .HasComment("FK al ítem que habilita al restringido.");

        // ── Índice único compuesto ────────────────────────────────────────────
        builder.HasIndex(r => new { r.IdRestrictedItem, r.IdEnablingItem })
            .IsUnique()
            .HasDatabaseName("UQ_productOptionItemAvailability_idRestrictedItem_idEnablingItem");

        // ── FK: restricted item ───────────────────────────────────────────────
        builder.HasOne(r => r.IdRestrictedItemNavigation)
            .WithMany(i => i.RestrictedByRules)
            .HasForeignKey(r => r.IdRestrictedItem)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: enabling item — sin cascade para evitar múltiples paths ───────
        builder.HasOne(r => r.IdEnablingItemNavigation)
            .WithMany(i => i.EnablesRules)
            .HasForeignKey(r => r.IdEnablingItem)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
