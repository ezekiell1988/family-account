using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class PurchaseInvoiceTypeConfiguration : IEntityTypeConfiguration<PurchaseInvoiceType>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceType> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo de tipos de factura de compra. Define si la contrapartida contable (CR) proviene del BankMovement vinculado o de una cuenta Caja fija por moneda."));

        builder.HasKey(pit => pit.IdPurchaseInvoiceType);
        builder.Property(pit => pit.IdPurchaseInvoiceType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del tipo de factura de compra.");

        builder.Property(pit => pit.CodePurchaseInvoiceType)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único del tipo: 'EFECTIVO', 'DEBITO', 'TC'.");

        builder.Property(pit => pit.NamePurchaseInvoiceType)
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("Nombre descriptivo del tipo de factura (ej. 'Tarjeta de Crédito').");

        builder.Property(pit => pit.CounterpartFromBankMovement)
            .IsRequired()
            .HasComment("true = la cuenta CR del asiento se toma del BankAccount vinculado al BankMovement (DEBITO, TC). false = la cuenta CR es fija (EFECTIVO: Caja CRC o Caja USD según moneda).");

        builder.Property(pit => pit.IdAccountCounterpartCRC)
            .HasComment("FK a la cuenta Caja CRC. Solo aplica cuando CounterpartFromBankMovement = false (tipo EFECTIVO). La selección entre CRC o USD se hace automáticamente según la moneda de la factura.");

        builder.Property(pit => pit.IdAccountCounterpartUSD)
            .HasComment("FK a la cuenta Caja USD. Solo aplica cuando CounterpartFromBankMovement = false (tipo EFECTIVO).");

        builder.Property(pit => pit.IdBankMovementType)
            .HasComment("FK al tipo de movimiento bancario usado para auto-crear el BankMovement al confirmar. Solo aplica cuando CounterpartFromBankMovement = true (DEBITO, TC).");

        builder.Property(pit => pit.IsActive)
            .IsRequired()
            .HasComment("Indica si el tipo de factura está activo y disponible para registrar nuevas facturas.");

        builder.HasIndex(pit => pit.CodePurchaseInvoiceType)
            .IsUnique()
            .HasDatabaseName("UQ_purchaseInvoiceType_codePurchaseInvoiceType");

        builder.HasIndex(pit => pit.IdAccountCounterpartCRC)
            .HasDatabaseName("IX_purchaseInvoiceType_idAccountCounterpartCRC")
            .HasFilter("[idAccountCounterpartCRC] IS NOT NULL");

        builder.HasIndex(pit => pit.IdAccountCounterpartUSD)
            .HasDatabaseName("IX_purchaseInvoiceType_idAccountCounterpartUSD")
            .HasFilter("[idAccountCounterpartUSD] IS NOT NULL");

        builder.HasIndex(pit => pit.IdBankMovementType)
            .HasDatabaseName("IX_purchaseInvoiceType_idBankMovementType")
            .HasFilter("[idBankMovementType] IS NOT NULL");

        builder.HasOne(pit => pit.IdAccountCounterpartCRCNavigation)
            .WithMany()
            .HasForeignKey(pit => pit.IdAccountCounterpartCRC)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pit => pit.IdAccountCounterpartUSDNavigation)
            .WithMany()
            .HasForeignKey(pit => pit.IdAccountCounterpartUSD)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pit => pit.IdBankMovementTypeNavigation)
            .WithMany()
            .HasForeignKey(pit => pit.IdBankMovementType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pit => pit.IdDefaultInventoryAccount)
            .HasComment("FK a la cuenta contable de inventario (DR) usada por defecto al confirmar líneas con producto. Si el producto tiene ProductAccount configurado, esa cuenta de gasto tendrá prioridad.");

        builder.HasIndex(pit => pit.IdDefaultInventoryAccount)
            .HasDatabaseName("IX_purchaseInvoiceType_idDefaultInventoryAccount")
            .HasFilter("[idDefaultInventoryAccount] IS NOT NULL");

        builder.HasOne(pit => pit.IdDefaultInventoryAccountNavigation)
            .WithMany()
            .HasForeignKey(pit => pit.IdDefaultInventoryAccount)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pit => pit.IdDefaultExpenseAccount)
            .HasComment("FK a la cuenta contable de gasto alternativa. Solo se usa cuando el producto tiene un ProductAccount explícito que apunta a ella (override de cuenta de gasto en lugar de inventario).");

        builder.HasIndex(pit => pit.IdDefaultExpenseAccount)
            .HasDatabaseName("IX_purchaseInvoiceType_idDefaultExpenseAccount")
            .HasFilter("[idDefaultExpenseAccount] IS NOT NULL");

        builder.HasOne(pit => pit.IdDefaultExpenseAccountNavigation)
            .WithMany()
            .HasForeignKey(pit => pit.IdDefaultExpenseAccount)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed inicial
        // EFECTIVO: CR fija → Caja CRC (IdAccount=106) o Caja USD (IdAccount=107) según moneda
        // DEBITO/TC: CR dinámica → BankAccount.IdAccount del BankMovement vinculado (FK nula por diseño)
        // IdDefaultInventoryAccount = 109 (1.1.07.01 Inventario de Mercadería) → cuenta DR por defecto para líneas con producto
        // IdDefaultExpenseAccount   = 75  (5.12.01 Gastos en Pareja) → alternativa solo cuando el producto tiene ProductAccount apuntando a ella
        builder.HasData(
            new PurchaseInvoiceType
            {
                IdPurchaseInvoiceType       = 1,
                CodePurchaseInvoiceType     = "EFECTIVO",
                NamePurchaseInvoiceType     = "Pago en Efectivo",
                CounterpartFromBankMovement = false,
                IdAccountCounterpartCRC     = 106,  // Caja CRC (₡) — 1.1.06.01
                IdAccountCounterpartUSD     = 107,  // Caja USD ($) — 1.1.06.02
                IdDefaultInventoryAccount   = 109,  // 1.1.07.01 Inventario de Mercadería (default DR)
                IdDefaultExpenseAccount     = 75,   // 5.12.01 Gastos en Pareja (override vía ProductAccount)
                IsActive                    = true
            },
            new PurchaseInvoiceType
            {
                IdPurchaseInvoiceType       = 2,
                CodePurchaseInvoiceType     = "DEBITO",
                NamePurchaseInvoiceType     = "Tarjeta de Débito / Transferencia",
                CounterpartFromBankMovement = true,
                IdAccountCounterpartCRC     = null,
                IdAccountCounterpartUSD     = null,
                IdBankMovementType          = 4,    // GASTO — Gasto General (Cargo)
                IdDefaultInventoryAccount   = 109,  // 1.1.07.01 Inventario de Mercadería (default DR)
                IdDefaultExpenseAccount     = 75,   // 5.12.01 Gastos en Pareja (override vía ProductAccount)
                IsActive                    = true
            },
            new PurchaseInvoiceType
            {
                IdPurchaseInvoiceType       = 3,
                CodePurchaseInvoiceType     = "TC",
                NamePurchaseInvoiceType     = "Tarjeta de Crédito",
                CounterpartFromBankMovement = true,
                IdAccountCounterpartCRC     = null,
                IdAccountCounterpartUSD     = null,
                IdBankMovementType          = 6,    // PAGO-TC — Pago Tarjeta de Crédito (Cargo)
                IdDefaultInventoryAccount   = 109,  // 1.1.07.01 Inventario de Mercadería (default DR)
                IdDefaultExpenseAccount     = 75,   // 5.12.01 Gastos en Pareja (override vía ProductAccount)
                IsActive                    = true
            });
    }
}
