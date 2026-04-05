using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceLineEntryConfiguration : IEntityTypeConfiguration<SalesInvoiceLineEntry>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLineEntry> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla pivot N:M salesInvoiceLine ↔ accountingEntryLine."));

        builder.HasKey(sile => sile.IdSalesInvoiceLineEntry);
        builder.Property(sile => sile.IdSalesInvoiceLineEntry)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental.");

        builder.HasIndex(sile => new { sile.IdSalesInvoiceLine, sile.IdAccountingEntryLine })
            .IsUnique()
            .HasDatabaseName("UQ_salesInvoiceLineEntry_idSalesInvoiceLine_idAccountingEntryLine");

        builder.HasOne(sile => sile.IdSalesInvoiceLineNavigation)
            .WithMany(sil => sil.SalesInvoiceLineEntries)
            .HasForeignKey(sile => sile.IdSalesInvoiceLine)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sile => sile.IdAccountingEntryLineNavigation)
            .WithMany()
            .HasForeignKey(sile => sile.IdAccountingEntryLine)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
