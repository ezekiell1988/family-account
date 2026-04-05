using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable(t => t.HasComment("Atributos definibles por producto padre que describen dimensiones de variación (ej: Talla, Color)"));

        builder.HasKey(a => a.IdProductAttribute);
        builder.Property(a => a.IdProductAttribute)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del atributo del producto");

        builder.Property(a => a.IdProduct)
            .IsRequired()
            .HasComment("Producto padre al que pertenece este atributo");

        builder.Property(a => a.NameAttribute)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Nombre del atributo (ej: Talla, Color, Material)");

        builder.Property(a => a.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Orden de presentación del atributo dentro del producto padre");

        builder.HasIndex(a => new { a.IdProduct, a.NameAttribute })
            .IsUnique()
            .HasDatabaseName("UQ_productAttribute_idProduct_nameAttribute");

        builder.HasOne(a => a.IdProductNavigation)
            .WithMany(p => p.ProductAttributes)
            .HasForeignKey(a => a.IdProduct)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
