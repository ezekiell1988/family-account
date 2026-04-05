using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class SalesInvoiceTypeConfiguration : IEntityTypeConfiguration<SalesInvoiceType>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceType> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo de tipos de factura de venta. Define contrapartida (Caja o BankMovement) y cuentas contables predeterminadas para ingresos y COGS."));

        builder.HasKey(sit => sit.IdSalesInvoiceType);
        builder.Property(sit => sit.IdSalesInvoiceType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del tipo de factura de venta.");

        builder.Property(sit => sit.CodeSalesInvoiceType)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único del tipo: 'CONTADO_CRC', 'CONTADO_USD', 'CREDITO_CRC', 'CREDITO_USD'.");

        builder.Property(sit => sit.NameSalesInvoiceType)
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("Nombre descriptivo del tipo de factura de venta.");

        builder.Property(sit => sit.CounterpartFromBankMovement)
            .IsRequired()
            .HasComment("true = la cuenta DR del asiento proviene del BankMovement vinculado; false = cuenta Caja fija por moneda.");

        builder.Property(sit => sit.IdAccountCounterpartCRC)
            .HasComment("FK a la cuenta Caja CRC. Solo aplica cuando CounterpartFromBankMovement = false.");

        builder.Property(sit => sit.IdAccountCounterpartUSD)
            .HasComment("FK a la cuenta Caja USD. Solo aplica cuando CounterpartFromBankMovement = false.");

        builder.Property(sit => sit.IdBankMovementType)
            .HasComment("FK al tipo de movimiento bancario para auto-crear el BankMovement al confirmar. Solo si CounterpartFromBankMovement = true.");

        builder.Property(sit => sit.IdAccountSalesRevenue)
            .HasComment("Cuenta CR de ingresos por ventas (fallback cuando el producto no tiene ProductAccount configurado).");

        builder.Property(sit => sit.IdAccountCOGS)
            .HasComment("Cuenta DR de costo de ventas (COGS) al reconocer el costo.");

        builder.Property(sit => sit.IdAccountInventory)
            .HasComment("Cuenta CR de inventario al reconocer el costo de ventas.");

        builder.Property(sit => sit.IsActive)
            .IsRequired()
            .HasComment("Indica si el tipo de factura de venta está activo.");

        builder.HasIndex(sit => sit.CodeSalesInvoiceType)
            .IsUnique()
            .HasDatabaseName("UQ_salesInvoiceType_codeSalesInvoiceType");

        builder.HasIndex(sit => sit.IdAccountCounterpartCRC)
            .HasFilter("[idAccountCounterpartCRC] IS NOT NULL")
            .HasDatabaseName("IX_salesInvoiceType_idAccountCounterpartCRC");

        builder.HasIndex(sit => sit.IdAccountCounterpartUSD)
            .HasFilter("[idAccountCounterpartUSD] IS NOT NULL")
            .HasDatabaseName("IX_salesInvoiceType_idAccountCounterpartUSD");

        builder.HasIndex(sit => sit.IdBankMovementType)
            .HasFilter("[idBankMovementType] IS NOT NULL")
            .HasDatabaseName("IX_salesInvoiceType_idBankMovementType");

        builder.HasOne(sit => sit.IdAccountCounterpartCRCNavigation)
            .WithMany()
            .HasForeignKey(sit => sit.IdAccountCounterpartCRC)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sit => sit.IdAccountCounterpartUSDNavigation)
            .WithMany()
            .HasForeignKey(sit => sit.IdAccountCounterpartUSD)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sit => sit.IdBankMovementTypeNavigation)
            .WithMany()
            .HasForeignKey(sit => sit.IdBankMovementType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sit => sit.IdAccountSalesRevenueNavigation)
            .WithMany()
            .HasForeignKey(sit => sit.IdAccountSalesRevenue)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sit => sit.IdAccountCOGSNavigation)
            .WithMany()
            .HasForeignKey(sit => sit.IdAccountCOGS)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sit => sit.IdAccountInventoryNavigation)
            .WithMany()
            .HasForeignKey(sit => sit.IdAccountInventory)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed inicial
        builder.HasData(
            new SalesInvoiceType
            {
                IdSalesInvoiceType          = 1,
                CodeSalesInvoiceType        = "CONTADO_CRC",
                NameSalesInvoiceType        = "Venta Contado CRC",
                CounterpartFromBankMovement = false,
                IdAccountCounterpartCRC     = 106,  // Caja CRC (₡) — 1.1.06.01
                IdAccountCounterpartUSD     = null,
                IdBankMovementType          = null,
                IdAccountSalesRevenue       = 117,  // 4.5.01 Ingresos por Ventas
                IdAccountCOGS              = 119,  // 5.15.01 Costo de Ventas — Mercadería
                IdAccountInventory          = 109,  // 1.1.07.01 Inventario de Mercadería
                IsActive                    = true
            },
            new SalesInvoiceType
            {
                IdSalesInvoiceType          = 2,
                CodeSalesInvoiceType        = "CONTADO_USD",
                NameSalesInvoiceType        = "Venta Contado USD",
                CounterpartFromBankMovement = false,
                IdAccountCounterpartCRC     = null,
                IdAccountCounterpartUSD     = 107,  // Caja USD ($) — 1.1.06.02
                IdBankMovementType          = null,
                IdAccountSalesRevenue       = 117,
                IdAccountCOGS              = 119,
                IdAccountInventory          = 109,
                IsActive                    = true
            },
            new SalesInvoiceType
            {
                IdSalesInvoiceType          = 3,
                CodeSalesInvoiceType        = "CREDITO_CRC",
                NameSalesInvoiceType        = "Venta a Crédito CRC (₡)",
                CounterpartFromBankMovement = true,
                IdAccountCounterpartCRC     = null,
                IdAccountCounterpartUSD     = null,
                IdBankMovementType          = 9,    // COBRO-CRC → contrapartida 1.1.08.01 CxC CRC
                IdAccountSalesRevenue       = 117,  // 4.5.01 Ingresos por Ventas
                IdAccountCOGS              = 119,  // 5.15.01 Costo de Ventas — Mercadería
                IdAccountInventory          = 109,  // 1.1.07.01 Inventario de Mercadería
                IsActive                    = true
            },
            new SalesInvoiceType
            {
                IdSalesInvoiceType          = 4,
                CodeSalesInvoiceType        = "CREDITO_USD",
                NameSalesInvoiceType        = "Venta a Crédito USD ($)",
                CounterpartFromBankMovement = true,
                IdAccountCounterpartCRC     = null,
                IdAccountCounterpartUSD     = null,
                IdBankMovementType          = 10,   // COBRO-USD → contrapartida 1.1.08.02 CxC USD
                IdAccountSalesRevenue       = 117,  // 4.5.01 Ingresos por Ventas
                IdAccountCOGS              = 119,  // 5.15.01 Costo de Ventas — Mercadería
                IdAccountInventory          = 109,  // 1.1.07.01 Inventario de Mercadería
                IsActive                    = true
            }
        );
    }
}
