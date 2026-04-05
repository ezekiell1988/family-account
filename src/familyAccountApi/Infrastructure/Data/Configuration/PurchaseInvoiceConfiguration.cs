using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Cabecera de factura de compra. Registra el gasto y genera automáticamente un asiento contable al confirmar.");
            t.HasCheckConstraint("CK_purchaseInvoice_statusInvoice", "statusInvoice IN ('Borrador', 'Confirmado', 'Anulado')");
        });

        builder.HasKey(pi => pi.IdPurchaseInvoice);
        builder.Property(pi => pi.IdPurchaseInvoice)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la factura de compra.");

        builder.Property(pi => pi.IdFiscalPeriod)
            .IsRequired()
            .HasComment("FK al período fiscal al que pertenece la factura de compra.");

        builder.Property(pi => pi.IdCurrency)
            .IsRequired()
            .HasComment("FK a la moneda de la factura. Para tipo EFECTIVO determina qué cuenta Caja usar (CRC o USD).");

        builder.Property(pi => pi.IdPurchaseInvoiceType)
            .IsRequired()
            .HasComment("FK al tipo de factura de compra (EFECTIVO, DEBITO, TC).");

        builder.Property(pi => pi.NumberInvoice)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Número de factura tal como aparece en el documento del proveedor.");

        builder.Property(pi => pi.ProviderName)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Snapshot del nombre del proveedor en el momento de la factura. Se autocompleta desde el contacto si se envía IdContact.");

        builder.Property(pi => pi.IdContact)
            .HasComment("FK al contacto proveedor. Si es nulo, el proveedor no está en el catálogo.");

        builder.Property(pi => pi.DateInvoice)
            .IsRequired()
            .HasComment("Fecha de emisión de la factura del proveedor.");

        builder.Property(pi => pi.SubTotalAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Subtotal de la factura antes de impuestos.");

        builder.Property(pi => pi.TaxAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Total de impuestos de la factura.");

        builder.Property(pi => pi.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Total general de la factura (SubTotalAmount + TaxAmount).");

        builder.Property(pi => pi.StatusInvoice)
            .HasMaxLength(15)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Borrador")
            .HasComment("Estado de la factura: 'Borrador', 'Confirmado' o 'Anulado'.");

        builder.Property(pi => pi.DescriptionInvoice)
            .HasMaxLength(500)
            .HasComment("Notas adicionales opcionales sobre la factura de compra.");

        builder.Property(pi => pi.ExchangeRateValue)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Tipo de cambio vigente al momento del registro de la factura.");

        builder.Property(pi => pi.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETDATE()")
            .HasComment("Fecha y hora de creación del registro.");

        builder.HasIndex(pi => pi.NumberInvoice)
            .IsUnique()
            .HasDatabaseName("UQ_purchaseInvoice_numberInvoice");

        builder.HasIndex(pi => pi.IdFiscalPeriod)
            .HasDatabaseName("IX_purchaseInvoice_idFiscalPeriod");

        builder.HasIndex(pi => pi.IdCurrency)
            .HasDatabaseName("IX_purchaseInvoice_idCurrency");

        builder.HasIndex(pi => pi.IdPurchaseInvoiceType)
            .HasDatabaseName("IX_purchaseInvoice_idPurchaseInvoiceType");

        builder.HasIndex(pi => pi.IdBankAccount)
            .HasDatabaseName("IX_purchaseInvoice_idBankAccount");

        builder.HasIndex(pi => pi.IdContact)
            .HasDatabaseName("IX_purchaseInvoice_idContact");

        builder.HasIndex(pi => pi.IdWarehouse)
            .HasFilter("[idWarehouse] IS NOT NULL")
            .HasDatabaseName("IX_purchaseInvoice_idWarehouse");

        builder.HasOne(pi => pi.IdFiscalPeriodNavigation)
            .WithMany()
            .HasForeignKey(pi => pi.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.IdCurrencyNavigation)
            .WithMany()
            .HasForeignKey(pi => pi.IdCurrency)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.IdPurchaseInvoiceTypeNavigation)
            .WithMany(pit => pit.PurchaseInvoices)
            .HasForeignKey(pi => pi.IdPurchaseInvoiceType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.IdBankAccountNavigation)
            .WithMany()
            .HasForeignKey(pi => pi.IdBankAccount)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.IdContactNavigation)
            .WithMany()
            .HasForeignKey(pi => pi.IdContact)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.IdWarehouseNavigation)
            .WithMany(w => w.PurchaseInvoices)
            .HasForeignKey(pi => pi.IdWarehouse)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.IdWarehouse)
            .HasComment("FK al almacén destino de la mercadería. Opcional; si es nulo al confirmar se usa el almacén predeterminado.");
    }
}
