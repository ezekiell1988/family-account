using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(t =>
            t.HasComment("Almacenes o bodegas de la empresa. El stock de inventario se segmenta por almacén a través de InventoryLot.idWarehouse."));

        builder.HasKey(w => w.IdWarehouse);
        builder.Property(w => w.IdWarehouse)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del almacén.");

        builder.Property(w => w.NameWarehouse)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode()
            .HasComment("Nombre descriptivo del almacén. Debe ser único.");

        builder.Property(w => w.IsDefault)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indica si este es el almacén predeterminado. Solo uno puede ser predeterminado a la vez. Se usa cuando no se especifica almacén al ingresar mercadería.");

        builder.Property(w => w.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Almacén activo. Los almacenes inactivos no aceptan nuevas entradas de stock.");

        builder.HasIndex(w => w.NameWarehouse)
            .IsUnique()
            .HasDatabaseName("UQ_warehouse_nameWarehouse");

        // Seed: almacén principal
        builder.HasData(new Warehouse
        {
            IdWarehouse   = 1,
            NameWarehouse = "Principal",
            IsDefault     = true,
            IsActive      = true
        });
    }
}
