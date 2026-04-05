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
            t.HasComment("Documento de ajuste de inventario. El tipo (idInventoryAdjustmentType) define las cuentas contables para generar el asiento al confirmar. Estados: Borrador → Confirmado → Anulado.");
            t.HasCheckConstraint("CK_inventoryAdjustment_statusAdjustment", "statusAdjustment IN ('Borrador', 'Confirmado', 'Anulado')");
        });

        builder.HasKey(ia => ia.IdInventoryAdjustment);
        builder.Property(ia => ia.IdInventoryAdjustment)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del ajuste.");

        builder.Property(ia => ia.IdFiscalPeriod)
            .IsRequired()
            .HasComment("FK al período fiscal al que corresponde este ajuste.");

        builder.Property(ia => ia.IdInventoryAdjustmentType)
            .IsRequired()
            .HasComment("FK al tipo de ajuste. Determina las cuentas contables del asiento generado al confirmar.");

        builder.Property(ia => ia.IdCurrency)
            .IsRequired()
            .HasComment("FK a la moneda del ajuste. Se usa en el asiento contable generado.");

        builder.Property(ia => ia.ExchangeRateValue)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Tipo de cambio vigente al momento del ajuste. 1.0 para moneda local.");

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

        builder.HasIndex(ia => ia.IdInventoryAdjustmentType)
            .HasDatabaseName("IX_inventoryAdjustment_idInventoryAdjustmentType");

        builder.HasIndex(ia => ia.IdCurrency)
            .HasDatabaseName("IX_inventoryAdjustment_idCurrency");

        builder.HasOne(ia => ia.IdFiscalPeriodNavigation)
            .WithMany()
            .HasForeignKey(ia => ia.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ia => ia.IdInventoryAdjustmentTypeNavigation)
            .WithMany(iat => iat.InventoryAdjustments)
            .HasForeignKey(ia => ia.IdInventoryAdjustmentType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ia => ia.IdCurrencyNavigation)
            .WithMany()
            .HasForeignKey(ia => ia.IdCurrency)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
