using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(t => t.HasComment("Catálogo interno de productos. Cada producto tiene un tipo (Materia Prima, Producto en Proceso, Producto Terminado, Reventa), una unidad base de inventario y opcionalmente un producto padre para agrupar variantes."));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(p => p.IdProduct);
        builder.Property(p => p.IdProduct)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del producto.");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(p => p.CodeProduct)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código interno único del producto definido por la empresa.");

        builder.Property(p => p.NameProduct)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre interno del producto usado en el sistema.");

        // ── Campos nuevos ───────────────────────────────────
        builder.Property(p => p.IdProductType)
            .IsRequired()
            .HasComment("FK al tipo de producto: Materia Prima, Prod. en Proceso, Prod. Terminado o Reventa.");

        builder.Property(p => p.IdUnit)
            .IsRequired()
            .HasComment("FK a la unidad de medida base del producto. Es la unidad en la que se lleva el inventario y se expresan las recetas.");

        builder.Property(p => p.IdProductParent)
            .HasComment("FK auto-referencial al producto padre. Agrupa variantes bajo un mismo producto (máximo un nivel). NULL si es raíz.");

        builder.Property(p => p.AverageCost)
            .HasPrecision(18, 6)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasComment("Costo promedio ponderado en unidad base. Se recalcula automáticamente al confirmar compras y ajustes con stock positivo.");

        builder.Property(p => p.RowVersion)
            .IsRowVersion()
            .HasComment("Token de concurrencia optimista. Previene race conditions al recalcular AverageCost en confirmaciones paralelas.");

        builder.Property(p => p.HasOptions)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indica que el producto tiene grupos de opciones configurables por el cliente (ej: tamaño, masa, sabor).");

        builder.Property(p => p.IsCombo)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indica que el producto es un combo compuesto de slots con productos elegibles.");

        // ── Índice único ─────────────────────────────────────
        builder.HasIndex(p => p.CodeProduct)
            .IsUnique()
            .HasDatabaseName("UQ_product_codeProduct");

        // ── FK: ProductType ─────────────────────────────────
        builder.HasOne(p => p.IdProductTypeNavigation)
            .WithMany(pt => pt.Products)
            .HasForeignKey(p => p.IdProductType)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: UnitOfMeasure (unidad base) ─────────────────
        builder.HasOne(p => p.IdUnitNavigation)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.IdUnit)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: Producto padre (auto-referencial) ────────────
        builder.HasOne(p => p.IdProductParentNavigation)
            .WithMany(p => p.Variants)
            .HasForeignKey(p => p.IdProductParent)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
