using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class AttributeValueConfiguration : IEntityTypeConfiguration<AttributeValue>
{
    public void Configure(EntityTypeBuilder<AttributeValue> builder)
    {
        builder.ToTable(t => t.HasComment("Valores posibles para cada atributo de producto padre (ej: S, M, L para el atributo Talla)"));

        builder.HasKey(v => v.IdAttributeValue);
        builder.Property(v => v.IdAttributeValue)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del valor de atributo");

        builder.Property(v => v.IdProductAttribute)
            .IsRequired()
            .HasComment("Atributo al que pertenece este valor");

        builder.Property(v => v.NameValue)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Nombre del valor (ej: S, M, L, Azul, Rojo)");

        builder.Property(v => v.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Orden de presentación del valor dentro del atributo");

        builder.HasIndex(v => new { v.IdProductAttribute, v.NameValue })
            .IsUnique()
            .HasDatabaseName("UQ_attributeValue_idProductAttribute_nameValue");

        builder.HasOne(v => v.IdProductAttributeNavigation)
            .WithMany(a => a.AttributeValues)
            .HasForeignKey(v => v.IdProductAttribute)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Seed: valores para atributos de Camisa Oxford ────────────────────
        // Talla (attr=1): S, M, L  |  Color (attr=2): Azul, Rojo
        builder.HasData(
            new AttributeValue { IdAttributeValue = 1, IdProductAttribute = 1, NameValue = "S",    SortOrder = 1 },
            new AttributeValue { IdAttributeValue = 2, IdProductAttribute = 1, NameValue = "M",    SortOrder = 2 },
            new AttributeValue { IdAttributeValue = 3, IdProductAttribute = 1, NameValue = "L",    SortOrder = 3 },
            new AttributeValue { IdAttributeValue = 4, IdProductAttribute = 2, NameValue = "Azul", SortOrder = 1 },
            new AttributeValue { IdAttributeValue = 5, IdProductAttribute = 2, NameValue = "Rojo", SortOrder = 2 }
        );
    }
}
