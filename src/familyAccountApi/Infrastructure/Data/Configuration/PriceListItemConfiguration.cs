using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable(t => t.HasComment("Ítem de lista de precios: precio unitario por producto y presentación (ProductUnit) dentro de una lista."));

        builder.HasKey(pli => pli.IdPriceListItem);
        builder.Property(pli => pli.IdPriceListItem)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del ítem.");

        builder.Property(pli => pli.IdPriceList)
            .IsRequired()
            .HasComment("FK a la lista de precios a la que pertenece este ítem.");

        builder.Property(pli => pli.IdProduct)
            .IsRequired()
            .HasComment("FK al producto.");

        builder.Property(pli => pli.IdProductUnit)
            .IsRequired()
            .HasComment("FK a la presentación (unidad de venta) del producto.");

        builder.Property(pli => pli.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Precio unitario del producto en esta presentación y lista.");

        builder.Property(pli => pli.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si el ítem está activo.");

        builder.Property(pli => pli.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");

        // Un mismo producto+presentación no puede aparecer dos veces en la misma lista
        builder.HasIndex(pli => new { pli.IdPriceList, pli.IdProduct, pli.IdProductUnit })
            .IsUnique()
            .HasDatabaseName("UQ_priceListItem_idPriceList_idProduct_idProductUnit");

        builder.HasOne(pli => pli.IdPriceListNavigation)
            .WithMany(pl => pl.PriceListItems)
            .HasForeignKey(pli => pli.IdPriceList)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pli => pli.IdProductNavigation)
            .WithMany()
            .HasForeignKey(pli => pli.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pli => pli.IdProductUnitNavigation)
            .WithMany()
            .HasForeignKey(pli => pli.IdProductUnit)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
