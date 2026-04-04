using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductComboSlotConfiguration : IEntityTypeConfiguration<ProductComboSlot>
{
    public void Configure(EntityTypeBuilder<ProductComboSlot> builder)
    {
        builder.ToTable(t => t.HasComment("Slots de un combo (ej: Pizza #1, Pizza #2, Bebida). Un producto con IsCombo=true tiene N slots."));

        builder.HasKey(s => s.IdProductComboSlot);
        builder.Property(s => s.IdProductComboSlot)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del slot del combo.");

        builder.Property(s => s.IdProductCombo)
            .IsRequired()
            .HasComment("FK al producto combo padre (IsCombo=true).");

        builder.Property(s => s.NameSlot)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre visible del slot (ej: Pizza #1, Bebida).");

        builder.Property(s => s.Quantity)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasDefaultValue(1m)
            .HasComment("Cantidad de este slot dentro del combo.");

        builder.Property(s => s.IsRequired)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Si el cliente debe llenar este slot obligatoriamente.");

        builder.Property(s => s.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Orden de presentación del slot al cliente.");

        // ── FK: Product (combo) ──────────────────────────────────────────────
        builder.HasOne(s => s.IdProductComboNavigation)
            .WithMany(p => p.ProductComboSlots)
            .HasForeignKey(s => s.IdProductCombo)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
