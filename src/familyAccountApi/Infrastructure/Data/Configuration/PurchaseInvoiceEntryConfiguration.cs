using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class PurchaseInvoiceEntryConfiguration : IEntityTypeConfiguration<PurchaseInvoiceEntry>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceEntry> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla auxiliar N:M entre purchaseInvoice y accountingEntry. Una factura puede vincularse a más de un asiento: el asiento inicial de confirmación y cualquier asiento de ajuste posterior. Nunca se modifica un asiento confirmado; se agregan nuevas filas en esta tabla."));

        builder.HasKey(pie => pie.IdPurchaseInvoiceEntry);
        builder.Property(pie => pie.IdPurchaseInvoiceEntry)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del vínculo factura-asiento.");

        builder.Property(pie => pie.IdPurchaseInvoice)
            .IsRequired()
            .HasComment("FK a la factura de compra.");

        builder.Property(pie => pie.IdAccountingEntry)
            .IsRequired()
            .HasComment("FK al asiento contable vinculado a la factura.");

        builder.HasIndex(pie => new { pie.IdPurchaseInvoice, pie.IdAccountingEntry })
            .IsUnique()
            .HasDatabaseName("UQ_purchaseInvoiceEntry_idPurchaseInvoice_idAccountingEntry");

        builder.HasIndex(pie => pie.IdAccountingEntry)
            .HasDatabaseName("IX_purchaseInvoiceEntry_idAccountingEntry");

        builder.HasOne(pie => pie.IdPurchaseInvoiceNavigation)
            .WithMany(pi => pi.PurchaseInvoiceEntries)
            .HasForeignKey(pie => pie.IdPurchaseInvoice)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pie => pie.IdAccountingEntryNavigation)
            .WithMany()
            .HasForeignKey(pie => pie.IdAccountingEntry)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
