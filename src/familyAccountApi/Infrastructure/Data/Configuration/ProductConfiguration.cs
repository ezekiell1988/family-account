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

        builder.Property(p => p.IsVariantParent)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indica que el producto es un padre que agrupa variantes por atributos (talla, color, etc.). Los padres no tienen stock propio.");

        builder.Property(p => p.ReorderPoint)
            .HasPrecision(12, 4)
            .HasComment("Punto de reorden: stock mínimo que dispara una alerta de reabastecimiento. NULL si no aplica.");

        builder.Property(p => p.SafetyStock)
            .HasPrecision(12, 4)
            .HasComment("Stock de seguridad reservado que no debe consumirse en operación normal. NULL si no aplica.");

        builder.Property(p => p.ReorderQuantity)
            .HasPrecision(12, 4)
            .HasComment("Cantidad sugerida a pedir cuando el stock cae por debajo del punto de reorden. NULL si no aplica.");

        builder.Property(p => p.ClassificationAbc)
            .HasMaxLength(1)
            .IsUnicode(false)
            .HasComment("Clasificación ABC calculada por Hangfire según valor de ventas de los últimos 90 días. A=top 80%, B=siguiente 15%, C=último 5%. NULL si sin ventas en el período.")
            .HasColumnType("CHAR(1)");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_product_classificationAbc",
            "[classificationAbc] IS NULL OR [classificationAbc] IN ('A', 'B', 'C')"));

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

        // ── Seed: productos demo para los 6 casos de uso ────
        // IdProductType: 1=MP, 2=ProcProceso, 3=PT, 4=Reventa
        // IdUnit: 1=U, 3=KG, 5=GR, 6=ML, 7=LTR
        builder.HasData(
            // C1 — Reventa (Coca-Cola)
            new Product { IdProduct =  1, CodeProduct = "REV-COCA-001",     NameProduct = "Coca-Cola 355ml",             IdProductType = 4, IdUnit = 1 },

            // C2 — Manufactura (Chile embotellado): materias primas
            new Product { IdProduct =  2, CodeProduct = "MP-CHILE-001",     NameProduct = "Chile Seco",                  IdProductType = 1, IdUnit = 3 },
            new Product { IdProduct =  3, CodeProduct = "MP-VINAGRE-001",   NameProduct = "Vinagre Blanco",              IdProductType = 1, IdUnit = 7 },
            new Product { IdProduct =  4, CodeProduct = "MP-SAL-001",       NameProduct = "Sal",                         IdProductType = 1, IdUnit = 3 },
            new Product { IdProduct =  5, CodeProduct = "MP-FRASCO-001",    NameProduct = "Frasco 250ml",                IdProductType = 1, IdUnit = 1 },
            // C2 — producto terminado
            new Product { IdProduct =  6, CodeProduct = "PT-CHILE-EMB-001", NameProduct = "Chile Embotellado Marca X",   IdProductType = 3, IdUnit = 1 },

            // C3 — Ensamble en venta (Hot dog): ingredientes
            new Product { IdProduct =  7, CodeProduct = "MP-PAN-HD-001",    NameProduct = "Pan de Hot Dog",              IdProductType = 1, IdUnit = 1 },
            new Product { IdProduct =  8, CodeProduct = "MP-SALCHICHA-001", NameProduct = "Salchicha",                   IdProductType = 1, IdUnit = 1 },
            new Product { IdProduct =  9, CodeProduct = "MP-MOSTAZA-001",   NameProduct = "Mostaza",                     IdProductType = 1, IdUnit = 6 },
            new Product { IdProduct = 10, CodeProduct = "MP-CATSUP-001",    NameProduct = "Catsup",                      IdProductType = 1, IdUnit = 6 },
            // C3 — producto ensamblado (PT con receta, sin producción previa)
            new Product { IdProduct = 11, CodeProduct = "PT-HOT-DOG-001",   NameProduct = "Hot Dog",                     IdProductType = 3, IdUnit = 1 },

            // C4 — Variantes (Camisa Oxford): padre + 5 variantes
            new Product { IdProduct = 12, CodeProduct = "CAMISA-OXF-000",   NameProduct = "Camisa Oxford",               IdProductType = 4, IdUnit = 1, IsVariantParent = true },
            new Product { IdProduct = 13, CodeProduct = "CAMISA-OXF-S-AZ",  NameProduct = "Camisa Oxford Talla S Azul",  IdProductType = 4, IdUnit = 1, IdProductParent = 12 },
            new Product { IdProduct = 14, CodeProduct = "CAMISA-OXF-M-AZ",  NameProduct = "Camisa Oxford Talla M Azul",  IdProductType = 4, IdUnit = 1, IdProductParent = 12 },
            new Product { IdProduct = 15, CodeProduct = "CAMISA-OXF-L-AZ",  NameProduct = "Camisa Oxford Talla L Azul",  IdProductType = 4, IdUnit = 1, IdProductParent = 12 },
            new Product { IdProduct = 16, CodeProduct = "CAMISA-OXF-S-RJ",  NameProduct = "Camisa Oxford Talla S Rojo",  IdProductType = 4, IdUnit = 1, IdProductParent = 12 },
            new Product { IdProduct = 17, CodeProduct = "CAMISA-OXF-M-RJ",  NameProduct = "Camisa Oxford Talla M Rojo",  IdProductType = 4, IdUnit = 1, IdProductParent = 12 },

            // C5 — Pedido configurado (Pizza): materias primas
            new Product { IdProduct = 18, CodeProduct = "MP-HARINA-001",    NameProduct = "Harina de Trigo",             IdProductType = 1, IdUnit = 3 },
            new Product { IdProduct = 19, CodeProduct = "MP-AGUA-001",      NameProduct = "Agua",                        IdProductType = 1, IdUnit = 7 },
            new Product { IdProduct = 20, CodeProduct = "MP-LEVADURA-001",  NameProduct = "Levadura",                    IdProductType = 1, IdUnit = 5 },
            new Product { IdProduct = 21, CodeProduct = "MP-ACEITE-001",    NameProduct = "Aceite de Oliva",             IdProductType = 1, IdUnit = 6 },
            new Product { IdProduct = 22, CodeProduct = "MP-SALSA-TOM-001", NameProduct = "Salsa de Tomate",             IdProductType = 1, IdUnit = 6 },
            new Product { IdProduct = 23, CodeProduct = "MP-MOZZ-001",      NameProduct = "Queso Mozzarella",            IdProductType = 1, IdUnit = 3 },
            new Product { IdProduct = 24, CodeProduct = "MP-PEPPERONI-001", NameProduct = "Pepperoni",                   IdProductType = 1, IdUnit = 3 },
            new Product { IdProduct = 25, CodeProduct = "MP-PINA-001",      NameProduct = "Piña en Rodajas",             IdProductType = 1, IdUnit = 3 },
            new Product { IdProduct = 26, CodeProduct = "MP-JAMON-001",     NameProduct = "Jamón",                       IdProductType = 1, IdUnit = 3 },
            // C5 — pizza configurable (PT con opciones)
            new Product { IdProduct = 27, CodeProduct = "PT-PIZZA-001",     NameProduct = "Pizza",                       IdProductType = 3, IdUnit = 1, HasOptions = true },

            // C6 — Combo (2 pizzas + bebida): bebidas adicionales + combo
            new Product { IdProduct = 28, CodeProduct = "REV-SPRITE-001",   NameProduct = "Sprite 355ml",                IdProductType = 4, IdUnit = 1 },
            new Product { IdProduct = 29, CodeProduct = "REV-AGUA-BOT-001", NameProduct = "Agua Pura Botella 500ml",     IdProductType = 4, IdUnit = 1 },
            new Product { IdProduct = 30, CodeProduct = "COMBO-2PIZ-BEB",   NameProduct = "Combo 2 Pizzas + Bebida",     IdProductType = 4, IdUnit = 1, IsCombo = true }
        );
    }
}
