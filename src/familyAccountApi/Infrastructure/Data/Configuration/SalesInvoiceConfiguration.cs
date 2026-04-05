using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Cabecera de la factura de venta. Flujo: Borrador → Confirmado (genera asiento + COGS + descuenta lote) → Anulado (revierte).");
            t.HasCheckConstraint("CK_salesInvoice_statusInvoice", "statusInvoice IN ('Borrador', 'Confirmado', 'Anulado')");
        });

        builder.HasKey(si => si.IdSalesInvoice);
        builder.Property(si => si.IdSalesInvoice)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la factura de venta.");

        builder.Property(si => si.NumberInvoice)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Número correlativo: FV-YYYYMMDD-NNN (asignado al confirmar; en Borrador = 'BORRADOR').");

        builder.Property(si => si.DateInvoice)
            .IsRequired()
            .HasComment("Fecha del documento de venta.");

        builder.Property(si => si.SubTotalAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Subtotal sin impuesto.");

        builder.Property(si => si.TaxAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto total de impuesto.");

        builder.Property(si => si.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Total del documento (subtotal + impuesto).");

        builder.Property(si => si.StatusInvoice)
            .HasMaxLength(15)
            .IsRequired()
            .IsUnicode(false)
            .HasDefaultValue("Borrador")
            .HasComment("Estado: Borrador | Confirmado | Anulado.");

        builder.Property(si => si.DescriptionInvoice)
            .HasMaxLength(500)
            .HasComment("Observaciones opcionales del documento.");

        builder.Property(si => si.ExchangeRateValue)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasComment("Tipo de cambio vigente al momento de la venta.");

        builder.Property(si => si.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");

        builder.HasIndex(si => si.NumberInvoice)
            .IsUnique()
            .HasDatabaseName("UQ_salesInvoice_numberInvoice");

        builder.HasIndex(si => si.IdFiscalPeriod)
            .HasDatabaseName("IX_salesInvoice_idFiscalPeriod");

        builder.HasIndex(si => si.IdContact)
            .HasFilter("[idContact] IS NOT NULL")
            .HasDatabaseName("IX_salesInvoice_idContact");

        builder.HasOne(si => si.IdFiscalPeriodNavigation)
            .WithMany()
            .HasForeignKey(si => si.IdFiscalPeriod)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.IdCurrencyNavigation)
            .WithMany()
            .HasForeignKey(si => si.IdCurrency)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.IdSalesInvoiceTypeNavigation)
            .WithMany(sit => sit.SalesInvoices)
            .HasForeignKey(si => si.IdSalesInvoiceType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.IdContactNavigation)
            .WithMany()
            .HasForeignKey(si => si.IdContact)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.IdBankAccountNavigation)
            .WithMany()
            .HasForeignKey(si => si.IdBankAccount)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(si => si.IdSalesOrder)
            .HasComment("FK al pedido de venta que origina esta factura. NULL = venta directa de tienda.");

        builder.HasIndex(si => si.IdSalesOrder)
            .HasFilter("[idSalesOrder] IS NOT NULL")
            .HasDatabaseName("IX_salesInvoice_idSalesOrder");

        builder.HasOne(si => si.IdSalesOrderNavigation)
            .WithMany(so => so.SalesInvoices)
            .HasForeignKey(si => si.IdSalesOrder)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
