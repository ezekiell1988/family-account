using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    public void Configure(EntityTypeBuilder<UnitType> builder)
    {
        builder.ToTable(t => t.HasComment("Clasificación dimensional de unidades de medida. Catálogo de sistema, sin CRUD expuesto al usuario."));

        builder.HasKey(ut => ut.IdUnitType);
        builder.Property(ut => ut.IdUnitType)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del tipo de unidad.");

        builder.Property(ut => ut.NameUnitType)
            .HasMaxLength(40)
            .IsRequired()
            .HasComment("Nombre del tipo dimensional: Unidad | Volumen | Masa | Longitud.");

        builder.HasIndex(ut => ut.NameUnitType)
            .IsUnique()
            .HasDatabaseName("UQ_unitType_nameUnitType");

        builder.HasData(
            new UnitType { IdUnitType = 1, NameUnitType = "Unidad"   },
            new UnitType { IdUnitType = 2, NameUnitType = "Volumen"  },
            new UnitType { IdUnitType = 3, NameUnitType = "Masa"     },
            new UnitType { IdUnitType = 4, NameUnitType = "Longitud" });
    }
}
