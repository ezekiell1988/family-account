using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable(t => t.HasComment("Tipo de producto según la fase productiva: Materia Prima, Producto en Proceso, Producto Terminado o Reventa. Catálogo de sistema, sin CRUD expuesto al usuario."));

        builder.HasKey(pt => pt.IdProductType);
        builder.Property(pt => pt.IdProductType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del tipo de producto.");

        builder.Property(pt => pt.NameProductType)
            .HasMaxLength(60)
            .IsRequired()
            .HasComment("Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa.");

        builder.Property(pt => pt.DescriptionProductType)
            .HasMaxLength(300)
            .HasComment("Descripción del tipo de producto y sus reglas de negocio.");

        builder.HasIndex(pt => pt.NameProductType)
            .IsUnique()
            .HasDatabaseName("UQ_productType_nameProductType");
    }
}
