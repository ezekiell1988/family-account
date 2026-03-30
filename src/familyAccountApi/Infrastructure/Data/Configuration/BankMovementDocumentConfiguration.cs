using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankMovementDocumentConfiguration : IEntityTypeConfiguration<BankMovementDocument>
{
    public void Configure(EntityTypeBuilder<BankMovementDocument> builder)
    {
        builder.ToTable(t =>
        {
            t.HasComment("Documentos de soporte vinculados a un movimiento bancario");
            t.HasCheckConstraint("CK_bankMovementDocument_typeDocument",
                "typeDocument IN ('FacturaCompra', 'Recibo', 'Transferencia', 'Cheque', 'Otro')");
        });

        builder.HasKey(bmd => bmd.IdBankMovementDocument);
        builder.Property(bmd => bmd.IdBankMovementDocument)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del documento de soporte");

        builder.Property(bmd => bmd.IdBankMovement)
            .IsRequired()
            .HasComment("FK al movimiento bancario al que pertenece el documento");

        builder.Property(bmd => bmd.IdPurchaseInvoice)
            .HasComment("FK opcional a la factura de compra vinculada a este documento de soporte");

        builder.Property(bmd => bmd.TypeDocument)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Tipo de documento: 'FacturaCompra', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'");

        builder.Property(bmd => bmd.NumberDocument)
            .HasMaxLength(100)
            .IsUnicode(false)
            .HasComment("Número o referencia del documento (factura, cheque, etc.)");

        builder.Property(bmd => bmd.DateDocument)
            .IsRequired()
            .HasComment("Fecha del documento de soporte");

        builder.Property(bmd => bmd.AmountDocument)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto del documento de soporte");

        builder.Property(bmd => bmd.DescriptionDocument)
            .HasMaxLength(500)
            .HasComment("Descripción adicional del documento");

        builder.HasIndex(bmd => bmd.IdBankMovement)
            .HasDatabaseName("IX_bankMovementDocument_idBankMovement");

        builder.HasIndex(bmd => bmd.IdPurchaseInvoice)
            .HasDatabaseName("IX_bankMovementDocument_idPurchaseInvoice")
            .HasFilter("[idPurchaseInvoice] IS NOT NULL");

        builder.HasOne(bmd => bmd.IdBankMovementNavigation)
            .WithMany(bm => bm.BankMovementDocuments)
            .HasForeignKey(bmd => bmd.IdBankMovement)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bmd => bmd.IdPurchaseInvoiceNavigation)
            .WithMany(pi => pi.BankMovementDocuments)
            .HasForeignKey(bmd => bmd.IdPurchaseInvoice)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
