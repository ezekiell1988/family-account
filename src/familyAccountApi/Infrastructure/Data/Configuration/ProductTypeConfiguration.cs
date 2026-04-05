using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable(t => t.HasComment("Tipo de producto según la fase productiva: Materia Prima, Producto en Proceso, Producto Terminado, Reventa o Servicios. Catálogo de sistema, sin CRUD expuesto al usuario."));

        builder.HasKey(pt => pt.IdProductType);
        builder.Property(pt => pt.IdProductType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del tipo de producto.");

        builder.Property(pt => pt.NameProductType)
            .HasMaxLength(60)
            .IsRequired()
            .HasComment("Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa | Servicios.");

        builder.Property(pt => pt.DescriptionProductType)
            .HasMaxLength(300)
            .HasComment("Descripción del tipo de producto y sus reglas de negocio.");

        builder.Property(pt => pt.TrackInventory)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si los productos de este tipo llevan control de stock (inventariables). false = Servicios y productos sin inventario.");

        builder.HasIndex(pt => pt.NameProductType)
            .IsUnique()
            .HasDatabaseName("UQ_productType_nameProductType");

        builder.HasData(
            new ProductType { IdProductType = 1, NameProductType = "Materia Prima",        TrackInventory = true,  DescriptionProductType = "Insumos o materiales adquiridos para ser utilizados en el proceso productivo. No se venden directamente." },
            new ProductType { IdProductType = 2, NameProductType = "Producto en Proceso",  TrackInventory = true,  DescriptionProductType = "Productos que han iniciado su proceso de fabricación pero aún no están terminados." },
            new ProductType { IdProductType = 3, NameProductType = "Producto Terminado",   TrackInventory = true,  DescriptionProductType = "Productos que han completado el proceso productivo y están listos para la venta." },
            new ProductType { IdProductType = 4, NameProductType = "Reventa",              TrackInventory = true,  DescriptionProductType = "Productos adquiridos listos para la venta sin transformación productiva." },
            new ProductType { IdProductType = 5, NameProductType = "Servicios",            TrackInventory = false, DescriptionProductType = "Servicios, mano de obra o conceptos sin stock físico. No generan movimientos de inventario." });
    }
}
