using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductRecipeConfiguration : IEntityTypeConfiguration<ProductRecipe>
{
    public void Configure(EntityTypeBuilder<ProductRecipe> builder)
    {
        builder.ToTable(t => t.HasComment("Recetas (BOM - Bill of Materials) para la producción de un producto. Define qué insumos se consumen y en qué cantidades para producir una corrida del output. Solo puede ser output un producto de tipo Producto en Proceso o Producto Terminado."));

        builder.HasKey(r => r.IdProductRecipe);
        builder.Property(r => r.IdProductRecipe)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la receta.");

        builder.Property(r => r.IdProductOutput)
            .IsRequired()
            .HasComment("FK al producto que produce esta receta. No puede ser Materia Prima ni Reventa.");

        builder.Property(r => r.NameRecipe)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre descriptivo de la receta (ej: Cahuita Salsa Caribeña 160ml v2).");

        builder.Property(r => r.QuantityOutput)
            .HasPrecision(12, 4)
            .IsRequired()
            .HasComment("Cantidad producida por corrida, expresada en la unidad base del producto output.");

        builder.Property(r => r.DescriptionRecipe)
            .HasMaxLength(500)
            .HasComment("Instrucciones generales u observaciones del proceso productivo.");

        builder.Property(r => r.VersionNumber)
            .IsRequired()
            .HasDefaultValue(1)
            .HasComment("Número de versión de la receta. Se incrementa al actualizar. Cada modificación crea una nueva fila; la anterior queda IsActive=false.");

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Solo recetas activas se usan en producción. Al actualizar una receta la versión anterior queda IsActive=false.");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación de esta versión.");

        builder.HasOne(r => r.IdProductOutputNavigation)
            .WithMany()
            .HasForeignKey(r => r.IdProductOutput)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.IdProductOutput, r.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UQ_productRecipe_idProductOutput_versionNumber");

        // ── Seed: recetas demo para los casos de uso ──────────────────────────
        // C2: Chile Embotellado (ProductOutput=6)
        // C3: Hot Dog (ProductOutput=11) — ensamble en venta
        // C5: Pizza (ProductOutput=27) — base + recetas de opciones (IsActive=false)
        var seedDate = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Unspecified);
        builder.HasData(
            new ProductRecipe { IdProductRecipe = 1, IdProductOutput =  6, VersionNumber = 1, NameRecipe = "Receta Chile Embotellado",  QuantityOutput = 1m,    IsActive = true,  CreatedAt = seedDate },
            new ProductRecipe { IdProductRecipe = 2, IdProductOutput = 11, VersionNumber = 1, NameRecipe = "Receta Hot Dog",            QuantityOutput = 1m,    IsActive = true,  CreatedAt = seedDate },
            new ProductRecipe { IdProductRecipe = 3, IdProductOutput = 27, VersionNumber = 1, NameRecipe = "Base Pizza",               QuantityOutput = 1m,    IsActive = true,  CreatedAt = seedDate },
            // Recetas de opciones (IsActive=false — solo se usan como fórmulas de ingredientes)
            new ProductRecipe { IdProductRecipe = 4, IdProductOutput = 27, VersionNumber = 2, NameRecipe = "Opción Sabor: Pepperoni",   QuantityOutput = 1m,    IsActive = false, CreatedAt = seedDate },
            new ProductRecipe { IdProductRecipe = 5, IdProductOutput = 27, VersionNumber = 3, NameRecipe = "Opción Sabor: Hawaiian",    QuantityOutput = 1m,    IsActive = false, CreatedAt = seedDate },
            new ProductRecipe { IdProductRecipe = 6, IdProductOutput = 27, VersionNumber = 4, NameRecipe = "Opción Tamaño: Grande",     QuantityOutput = 1m,    IsActive = false, CreatedAt = seedDate },
            new ProductRecipe { IdProductRecipe = 7, IdProductOutput = 27, VersionNumber = 5, NameRecipe = "Opción Extra: Doble Queso", QuantityOutput = 1m,    IsActive = false, CreatedAt = seedDate }
        );
    }
}
