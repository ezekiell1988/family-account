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

        // ── Seed: vínculo variante ↔ valor de atributo ─────────────────────────
        // IdProduct: 13=S-Azul, 14=M-Azul, 15=L-Azul, 16=S-Rojo, 17=M-Rojo
        // IdAttributeValue: 1=S, 2=M, 3=L, 4=Azul, 5=Rojo
        builder.HasData(
            new ProductVariantAttribute { IdProductVariantAttribute =  1, IdProduct = 13, IdAttributeValue = 1 },  // S-Azul → S
            new ProductVariantAttribute { IdProductVariantAttribute =  2, IdProduct = 13, IdAttributeValue = 4 },  // S-Azul → Azul
            new ProductVariantAttribute { IdProductVariantAttribute =  3, IdProduct = 14, IdAttributeValue = 2 },  // M-Azul → M
            new ProductVariantAttribute { IdProductVariantAttribute =  4, IdProduct = 14, IdAttributeValue = 4 },  // M-Azul → Azul
            new ProductVariantAttribute { IdProductVariantAttribute =  5, IdProduct = 15, IdAttributeValue = 3 },  // L-Azul → L
            new ProductVariantAttribute { IdProductVariantAttribute =  6, IdProduct = 15, IdAttributeValue = 4 },  // L-Azul → Azul
            new ProductVariantAttribute { IdProductVariantAttribute =  7, IdProduct = 16, IdAttributeValue = 1 },  // S-Rojo → S
            new ProductVariantAttribute { IdProductVariantAttribute =  8, IdProduct = 16, IdAttributeValue = 5 },  // S-Rojo → Rojo
            new ProductVariantAttribute { IdProductVariantAttribute =  9, IdProduct = 17, IdAttributeValue = 2 },  // M-Rojo → M
            new ProductVariantAttribute { IdProductVariantAttribute = 10, IdProduct = 17, IdAttributeValue = 5 }   // M-Rojo → Rojo
        );
    }
}
