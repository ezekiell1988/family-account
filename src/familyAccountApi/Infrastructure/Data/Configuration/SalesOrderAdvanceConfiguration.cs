using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesOrderAdvanceConfiguration : IEntityTypeConfiguration<SalesOrderAdvance>
{
    public void Configure(EntityTypeBuilder<SalesOrderAdvance> builder)
    {
        builder.ToTable(t => t.HasComment("Anticipo o depósito recibido de un cliente contra un pedido de venta. Se aplica como crédito al emitir la SalesInvoice final. IdProductionOrder es contexto informativo sobre cuándo/por qué se recibió el anticipo."));

        builder.HasKey(a => a.IdSalesOrderAdvance);
        builder.Property(a => a.IdSalesOrderAdvance)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del anticipo.");

        builder.Property(a => a.IdSalesOrder)
            .IsRequired()
            .HasComment("FK al pedido de venta al que corresponde este anticipo.");

        builder.Property(a => a.IdAccountingEntry)
            .IsRequired()
            .HasComment("FK al asiento contable que registra la recepción del anticipo.");

        builder.Property(a => a.IdProductionOrder)
            .HasComment("FK informativa a la orden de producción en cuyo contexto se recibió el anticipo. No afecta la lógica financiera.");

        builder.Property(a => a.Amount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasComment("Monto del anticipo en la moneda del pedido.");

        builder.Property(a => a.DateAdvance)
            .IsRequired()
            .HasComment("Fecha en que se recibió el anticipo.");

        builder.Property(a => a.DescriptionAdvance)
            .HasMaxLength(500)
            .HasComment("Nota opcional sobre el anticipo.");

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");

        builder.HasIndex(a => a.IdSalesOrder)
            .HasDatabaseName("IX_salesOrderAdvance_idSalesOrder");

        builder.HasIndex(a => a.IdProductionOrder)
            .HasFilter("[idProductionOrder] IS NOT NULL")
            .HasDatabaseName("IX_salesOrderAdvance_idProductionOrder");

        builder.HasOne(a => a.IdSalesOrderNavigation)
            .WithMany(so => so.SalesOrderAdvances)
            .HasForeignKey(a => a.IdSalesOrder)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.IdAccountingEntryNavigation)
            .WithMany()
            .HasForeignKey(a => a.IdAccountingEntry)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.IdProductionOrderNavigation)
            .WithMany(po => po.SalesOrderAdvances)
            .HasForeignKey(a => a.IdProductionOrder)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
