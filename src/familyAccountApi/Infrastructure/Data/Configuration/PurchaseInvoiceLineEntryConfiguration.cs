using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class PurchaseInvoiceLineEntryConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLineEntry>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceLineEntry> builder)
    {
        builder.ToTable(t => t.HasComment("Tabla auxiliar N:M entre purchaseInvoiceLine y accountingEntryLine. Permite trazar qué líneas del asiento contable se originaron de cada línea de factura. Una línea de factura genera N líneas contables según la distribución de ProductAccount."));

        builder.HasKey(pile => pile.IdPurchaseInvoiceLineEntry);
        builder.Property(pile => pile.IdPurchaseInvoiceLineEntry)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del vínculo línea-factura/línea-asiento.");

        builder.Property(pile => pile.IdPurchaseInvoiceLine)
            .IsRequired()
            .HasComment("FK a la línea de factura de compra.");

        builder.Property(pile => pile.IdAccountingEntryLine)
            .IsRequired()
            .HasComment("FK a la línea del asiento contable generada a partir de esta línea de factura.");

        builder.HasIndex(pile => new { pile.IdPurchaseInvoiceLine, pile.IdAccountingEntryLine })
            .IsUnique()
            .HasDatabaseName("UQ_purchaseInvoiceLineEntry_idPurchaseInvoiceLine_idAccountingEntryLine");

        builder.HasIndex(pile => pile.IdAccountingEntryLine)
            .HasDatabaseName("IX_purchaseInvoiceLineEntry_idAccountingEntryLine");

        builder.HasOne(pile => pile.IdPurchaseInvoiceLineNavigation)
            .WithMany(pil => pil.PurchaseInvoiceLineEntries)
            .HasForeignKey(pile => pile.IdPurchaseInvoiceLine)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pile => pile.IdAccountingEntryLineNavigation)
            .WithMany()
            .HasForeignKey(pile => pile.IdAccountingEntryLine)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
