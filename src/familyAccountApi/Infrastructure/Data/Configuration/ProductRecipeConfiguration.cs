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

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Solo recetas activas se usan en producción.");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");

        builder.HasOne(r => r.IdProductOutputNavigation)
            .WithMany()
            .HasForeignKey(r => r.IdProductOutput)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
