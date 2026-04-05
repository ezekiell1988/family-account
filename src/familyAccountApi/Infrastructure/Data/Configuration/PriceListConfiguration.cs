using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable(t => t.HasComment("Lista de precios con vigencia por fechas. Al crear un pedido se hace snapshot del precio vigente en SalesOrderLine.UnitPrice."));

        builder.HasKey(pl => pl.IdPriceList);
        builder.Property(pl => pl.IdPriceList)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la lista de precios.");

        builder.Property(pl => pl.NamePriceList)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre descriptivo de la lista (ej: Lista Mayorista Abril 2026).");

        builder.Property(pl => pl.Description)
            .HasMaxLength(500)
            .HasComment("Descripción opcional de la lista.");

        builder.Property(pl => pl.DateFrom)
            .IsRequired()
            .HasComment("Fecha de inicio de vigencia.");

        builder.Property(pl => pl.DateTo)
            .HasComment("Fecha de fin de vigencia. NULL = vigente indefinidamente.");

        builder.Property(pl => pl.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si la lista está activa para su uso.");

        builder.Property(pl => pl.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha y hora UTC de creación del registro.");
    }
}
