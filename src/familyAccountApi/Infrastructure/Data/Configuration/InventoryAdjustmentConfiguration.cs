using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Documento de ajuste de inventario. Cubre tres casos: Conteo Físico (corrección teórico vs real), Producción (corrida V1: consume MP/PP y genera PP/PT) y Ajuste de Costo (corrige unitCost sin mover cantidades). Único mecanismo válido para modificar inventoryLot fuera de una factura.");
            t.HasCheckConstraint("CK_inventoryAdjustment_typeAdjustment", "typeAdjustment IN ('Conteo Físico', 'Producción', 'Ajuste de Costo')");
            t.HasCheckConstraint("CK_inventoryAdjustment_statusAdjustment", "statusAdjustment IN ('Borrador', 'Confirmado', 'Anulado')");
        });

        builder.HasKey(ia => ia.IdInventoryAdjustment);
        builder.Property(ia => ia.IdInventoryAdjustment)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del ajuste.");

        builder.Property(ia => ia.IdFiscalPeriod)
            .IsRequired()
            .HasComment("FK al período fiscal al que corresponde este ajuste.");

        builder.Property(ia => ia.TypeAdjustment)
            .HasMaxLength(20)
            .IsRequired()
            .HasComment("Tipo de ajuste: Conteo Físico | Producción | Ajuste de Costo.");

        builder.Property(ia => ia.NumberAdjustment)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Consecutivo interno generado al confirmar. Formato: AJ-YYYYMMDD-NNN.");

        builder.Property(ia => ia.DateAdjustment)
            .IsRequired()
            .HasComment("Fecha del evento: conteo físico, corrida de producción o ajuste de costo.");

        builder.Property(ia => ia.DescriptionAdjustment)
            .HasMaxLength(500)
            .HasComment("Motivo o descripción del ajuste (ej: Conteo físico mensual, Corrida lote 26032002, NC proveedor).");

        builder.Property(ia => ia.StatusAdjustment)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Estado del ajuste: Borrador | Confirmado | Anulado.");

        builder.Property(ia => ia.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");

        builder.HasIndex(ia => ia.NumberAdjustment)
            .IsUnique()
            .HasDatabaseName("UQ_inventoryAdjustment_numberAdjustment");

        builder.HasIndex(ia => ia.IdFiscalPeriod)
            .HasDatabaseName("IX_inventoryAdjustment_idFiscalPeriod");

        builder.HasOne(ia => ia.IdFiscalPeriodNavigation)
            .WithMany()
            .HasForeignKey(ia => ia.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
