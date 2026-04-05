using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductComboSlotPresetOptionConfiguration : IEntityTypeConfiguration<ProductComboSlotPresetOption>
{
    public void Configure(EntityTypeBuilder<ProductComboSlotPresetOption> builder)
    {
        builder.ToTable(t => t.HasComment("Opciones preseleccionadas en el catálogo para un slot de combo. El cliente las ve bloqueadas (no editables)."));

        builder.HasKey(p => p.IdProductComboSlotPresetOption);
        builder.Property(p => p.IdProductComboSlotPresetOption)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.Property(p => p.IdProductComboSlot)
            .IsRequired()
            .HasComment("FK al slot del combo al que pertenece esta opción preset.");

        builder.Property(p => p.IdProductOptionItem)
            .IsRequired()
            .HasComment("FK al ítem de opción preseleccionado (debe pertenecer al producto del slot).");

        // ── Índice único: sin duplicados por slot ────────────────────────────
        builder.HasIndex(p => new { p.IdProductComboSlot, p.IdProductOptionItem })
            .IsUnique()
            .HasDatabaseName("UQ_productComboSlotPresetOption_slot_item");

        // ── FK: ProductComboSlot ─────────────────────────────────────────────
        builder.HasOne(p => p.IdProductComboSlotNavigation)
            .WithMany(s => s.PresetOptions)
            .HasForeignKey(p => p.IdProductComboSlot)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: ProductOptionItem ────────────────────────────────────────────
        builder.HasOne(p => p.IdProductOptionItemNavigation)
            .WithMany()
            .HasForeignKey(p => p.IdProductOptionItem)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Seed: opciones preseleccionadas en el combo ───────────────────────
        // Ambas pizzas del combo tienen Tamaño=Grande (IdProductOptionItem=2) prefijado
        builder.HasData(
            new ProductComboSlotPresetOption { IdProductComboSlotPresetOption = 1, IdProductComboSlot = 1, IdProductOptionItem = 2 },  // Pizza #1 → Grande
            new ProductComboSlotPresetOption { IdProductComboSlotPresetOption = 2, IdProductComboSlot = 2, IdProductOptionItem = 2 }   // Pizza #2 → Grande
        );
    }
}
