using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class InventoryAdjustmentTypeConfiguration : IEntityTypeConfiguration<InventoryAdjustmentType>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustmentType> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo de tipos de ajuste de inventario. Define las cuentas contables usadas para generar el asiento al confirmar un ajuste: cuenta de inventario (activo), cuenta contrapartida de entrada y cuenta contrapartida de salida."));

        builder.HasKey(iat => iat.IdInventoryAdjustmentType);
        builder.Property(iat => iat.IdInventoryAdjustmentType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del tipo de ajuste.");

        builder.Property(iat => iat.CodeInventoryAdjustmentType)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único del tipo: 'CONTEO', 'PRODUCCION', 'AJUSTE_COSTO'.");

        builder.Property(iat => iat.NameInventoryAdjustmentType)
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("Nombre descriptivo del tipo (ej. 'Conteo Físico', 'Producción', 'Ajuste de Costo').");

        builder.Property(iat => iat.IdAccountInventoryDefault)
            .HasComment("FK a la cuenta de activo de inventario. DR en entradas (delta+), CR en salidas (delta-). Si es null, no se genera asiento contable al confirmar.");

        builder.Property(iat => iat.IdAccountCounterpartEntry)
            .HasComment("FK a la cuenta contrapartida para entradas (delta > 0) o ajuste de costo al alza. Actúa como CR del asiento. Ej: 'Ajuste Favorable de Inventario'.");

        builder.Property(iat => iat.IdAccountCounterpartExit)
            .HasComment("FK a la cuenta contrapartida para salidas (delta < 0) o ajuste de costo a la baja. Actúa como DR del asiento. Ej: 'Gasto por Merma', 'Costo de Producción'.");

        builder.Property(iat => iat.IsActive)
            .IsRequired()
            .HasComment("Indica si el tipo está activo y disponible para nuevos ajustes.");

        builder.HasIndex(iat => iat.CodeInventoryAdjustmentType)
            .IsUnique()
            .HasDatabaseName("UQ_inventoryAdjustmentType_codeInventoryAdjustmentType");

        builder.HasIndex(iat => iat.IdAccountInventoryDefault)
            .HasDatabaseName("IX_inventoryAdjustmentType_idAccountInventoryDefault")
            .HasFilter("[idAccountInventoryDefault] IS NOT NULL");

        builder.HasIndex(iat => iat.IdAccountCounterpartEntry)
            .HasDatabaseName("IX_inventoryAdjustmentType_idAccountCounterpartEntry")
            .HasFilter("[idAccountCounterpartEntry] IS NOT NULL");

        builder.HasIndex(iat => iat.IdAccountCounterpartExit)
            .HasDatabaseName("IX_inventoryAdjustmentType_idAccountCounterpartExit")
            .HasFilter("[idAccountCounterpartExit] IS NOT NULL");

        builder.HasOne(iat => iat.IdAccountInventoryDefaultNavigation)
            .WithMany()
            .HasForeignKey(iat => iat.IdAccountInventoryDefault)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(iat => iat.IdAccountCounterpartEntryNavigation)
            .WithMany()
            .HasForeignKey(iat => iat.IdAccountCounterpartEntry)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(iat => iat.IdAccountCounterpartExitNavigation)
            .WithMany()
            .HasForeignKey(iat => iat.IdAccountCounterpartExit)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed: 3 tipos con cuentas contables ya configuradas
        // Cuentas inventario:    109 = Inventario de Mercadería, 110 = Materias Primas, 111 = Productos en Proceso
        // Cuentas contrapartida: 129 = Merma Normal (IAS 2.16), 130 = Merma Anormal (IAS 2.16),
        //                        114 = Sobrantes, 115 = Costos de Producción
        builder.HasData(
            new InventoryAdjustmentType
            {
                IdInventoryAdjustmentType   = 1,
                CodeInventoryAdjustmentType = "CONTEO",
                NameInventoryAdjustmentType = "Conteo Físico",
                IdAccountInventoryDefault   = 109,  // 1.1.07.01 Inventario de Mercadería
                IdAccountCounterpartEntry   = 114,  // 5.14.02 Sobrantes de Inventario (CR)
                IdAccountCounterpartExit    = 130,  // 5.14.01.02 Merma Anormal (DR) — IAS 2.16
                IsActive                   = true
            },
            new InventoryAdjustmentType
            {
                IdInventoryAdjustmentType   = 2,
                CodeInventoryAdjustmentType = "PRODUCCION",
                NameInventoryAdjustmentType = "Producción",
                IdAccountInventoryDefault   = 111,  // 1.1.07.03 Productos en Proceso
                IdAccountCounterpartEntry   = 115,  // 5.14.03 Costos de Producción (CR consumo MP)
                IdAccountCounterpartExit    = 115,  // 5.14.03 Costos de Producción (DR salida MP)
                IsActive                   = true
            },
            new InventoryAdjustmentType
            {
                IdInventoryAdjustmentType   = 3,
                CodeInventoryAdjustmentType = "AJUSTE_COSTO",
                NameInventoryAdjustmentType = "Ajuste de Costo",
                IdAccountInventoryDefault   = 109,  // 1.1.07.01 Inventario de Mercadería
                IdAccountCounterpartEntry   = 114,  // 5.14.02 Sobrantes / ajuste favorable (CR)
                IdAccountCounterpartExit    = 130,  // 5.14.01.02 Merma Anormal / ajuste desfavorable (DR) — IAS 2.16
                IsActive                   = true
            },
            new InventoryAdjustmentType
            {
                IdInventoryAdjustmentType   = 4,
                CodeInventoryAdjustmentType = "REGALIA",
                NameInventoryAdjustmentType = "Regalía",
                IdAccountInventoryDefault   = 109,  // 1.1.07.01 Inventario de Mercadería
                IdAccountCounterpartEntry   = null, // No hay ajuste favorable en regalías
                IdAccountCounterpartExit    = 130,  // 5.14.01.02 Merma Anormal (DR) — IAS 2.16
                IsActive                   = true
            });
    }
}
