using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceEntryConfiguration : IEntityTypeConfiguration<SalesInvoiceEntry>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceEntry> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla pivot N:M salesInvoice ↔ accountingEntry."));

        builder.HasKey(sie => sie.IdSalesInvoiceEntry);
        builder.Property(sie => sie.IdSalesInvoiceEntry)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.HasIndex(sie => new { sie.IdSalesInvoice, sie.IdAccountingEntry })
            .IsUnique()
            .HasDatabaseName("UQ_salesInvoiceEntry_idSalesInvoice_idAccountingEntry");

        builder.HasOne(sie => sie.IdSalesInvoiceNavigation)
            .WithMany(si => si.SalesInvoiceEntries)
            .HasForeignKey(sie => sie.IdSalesInvoice)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sie => sie.IdAccountingEntryNavigation)
            .WithMany()
            .HasForeignKey(sie => sie.IdAccountingEntry)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
