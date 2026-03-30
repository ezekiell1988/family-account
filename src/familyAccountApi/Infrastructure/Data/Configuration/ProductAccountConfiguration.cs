using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductAccountConfiguration : IEntityTypeConfiguration<ProductAccount>
{
    public void Configure(EntityTypeBuilder<ProductAccount> builder)
    {
        builder.ToTable(t => t.HasComment("Distribución contable por producto: define la cuenta de gasto y el centro de costo para cada porcentaje del total de la línea de factura. La suma de PercentageAccount por IdProduct debe ser exactamente 100."));

        builder.HasKey(pa => pa.IdProductAccount);
        builder.Property(pa => pa.IdProductAccount)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la distribución contable del producto.");

        builder.Property(pa => pa.IdProduct)
            .IsRequired()
            .HasComment("FK al producto que se está distribuyendo contablemente.");

        builder.Property(pa => pa.IdAccount)
            .IsRequired()
            .HasComment("FK a la cuenta contable de gasto (DR del asiento de factura).");

        builder.Property(pa => pa.IdCostCenter)
            .HasComment("FK opcional al centro de costo. Nullable cuando el producto no requiere distribución por centro de costo.");

        builder.Property(pa => pa.PercentageAccount)
            .HasPrecision(5, 2)
            .IsRequired()
            .HasComment("Porcentaje del total de la línea asignado a esta cuenta/centro de costo. La suma por IdProduct debe ser 100.00.");

        builder.HasIndex(pa => new { pa.IdProduct, pa.IdAccount, pa.IdCostCenter })
            .IsUnique()
            .HasDatabaseName("UQ_productAccount_idProduct_idAccount_idCostCenter");

        builder.HasIndex(pa => pa.IdAccount)
            .HasDatabaseName("IX_productAccount_idAccount");

        builder.HasIndex(pa => pa.IdCostCenter)
            .HasDatabaseName("IX_productAccount_idCostCenter")
            .HasFilter("[idCostCenter] IS NOT NULL");

        builder.HasOne(pa => pa.IdProductNavigation)
            .WithMany()
            .HasForeignKey(pa => pa.IdProduct)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.IdAccountNavigation)
            .WithMany()
            .HasForeignKey(pa => pa.IdAccount)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pa => pa.IdCostCenterNavigation)
            .WithMany()
            .HasForeignKey(pa => pa.IdCostCenter)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
