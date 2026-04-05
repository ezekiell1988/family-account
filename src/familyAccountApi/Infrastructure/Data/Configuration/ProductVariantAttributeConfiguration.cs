using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductVariantAttributeConfiguration : IEntityTypeConfiguration<ProductVariantAttribute>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttribute> builder)
    {
        builder.ToTable(t => t.HasComment("Vincula una variante hija con los valores de atributo que la definen (ej: Camisa Oxford M + Azul)"));

        builder.HasKey(va => va.IdProductVariantAttribute);
        builder.Property(va => va.IdProductVariantAttribute)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del vínculo variante-atributo");

        builder.Property(va => va.IdProduct)
            .IsRequired()
            .HasComment("Producto variante hijo al que pertenece este vínculo");

        builder.Property(va => va.IdAttributeValue)
            .IsRequired()
            .HasComment("Valor de atributo que forma parte de la combinación de esta variante");

        builder.HasIndex(va => new { va.IdProduct, va.IdAttributeValue })
            .IsUnique()
            .HasDatabaseName("UQ_productVariantAttribute_idProduct_idAttributeValue");

        builder.HasOne(va => va.IdProductNavigation)
            .WithMany(p => p.ProductVariantAttributes)
            .HasForeignKey(va => va.IdProduct)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(va => va.IdAttributeValueNavigation)
            .WithMany(av => av.ProductVariantAttributes)
            .HasForeignKey(va => va.IdAttributeValue)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
