using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        // ── Comentario de tabla + check constraint ───────────────────────────
        builder.ToTable(t =>
        {
            t.HasComment("Períodos fiscales del sistema contable. Cada período representa un mes de un año y controla si se permiten movimientos contables en ese período.");
            t.HasCheckConstraint("CK_fiscalPeriod_statusPeriod", "statusPeriod IN ('Abierto', 'Cerrado', 'Bloqueado')");
        });

        // ── PK ───────────────────────────────────────────────────────────────
        builder.HasKey(fp => fp.IdFiscalPeriod);
        builder.Property(fp => fp.IdFiscalPeriod)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental del período fiscal.");

        // ── Año ──────────────────────────────────────────────────────────────
        builder.Property(fp => fp.YearPeriod)
            .IsRequired()
            .HasComment("Año calendario del período fiscal (p. ej. 2026).");

        // ── Mes ──────────────────────────────────────────────────────────────
        builder.Property(fp => fp.MonthPeriod)
            .IsRequired()
            .HasComment("Número de mes del período fiscal: 1=Enero, ..., 12=Diciembre.");

        // ── Nombre ───────────────────────────────────────────────────────────
        builder.Property(fp => fp.NamePeriod)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Nombre descriptivo del período (p. ej. 'Enero 2026').");

        // ── Estado ───────────────────────────────────────────────────────────
        builder.Property(fp => fp.StatusPeriod)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Estado del período: 'Abierto' permite movimientos, 'Cerrado' no admite nuevos registros, 'Bloqueado' está bloqueado administrativamente.");

        // ── Fecha de inicio ──────────────────────────────────────────────────
        builder.Property(fp => fp.StartDate)
            .IsRequired()
            .HasComment("Fecha de inicio del período fiscal (primer día del mes).");

        // ── Fecha de fin ─────────────────────────────────────────────────────
        builder.Property(fp => fp.EndDate)
            .IsRequired()
            .HasComment("Fecha de fin del período fiscal (último día del mes).");

        // ── Índice único ─────────────────────────────────────────────────────
        builder.HasIndex(fp => new { fp.YearPeriod, fp.MonthPeriod })
            .IsUnique()
            .HasDatabaseName("UQ_fiscalPeriod_yearPeriod_monthPeriod");

        // ── Seed: año 2026 (enero a diciembre) ──────────────────────────────
        builder.HasData(
            new FiscalPeriod { IdFiscalPeriod =  1, YearPeriod = 2026, MonthPeriod =  1, NamePeriod = "Enero 2026",      StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  1,  1), EndDate = new DateOnly(2026,  1, 31) },
            new FiscalPeriod { IdFiscalPeriod =  2, YearPeriod = 2026, MonthPeriod =  2, NamePeriod = "Febrero 2026",    StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  2,  1), EndDate = new DateOnly(2026,  2, 28) },
            new FiscalPeriod { IdFiscalPeriod =  3, YearPeriod = 2026, MonthPeriod =  3, NamePeriod = "Marzo 2026",      StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  3,  1), EndDate = new DateOnly(2026,  3, 31) },
            new FiscalPeriod { IdFiscalPeriod =  4, YearPeriod = 2026, MonthPeriod =  4, NamePeriod = "Abril 2026",      StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  4,  1), EndDate = new DateOnly(2026,  4, 30) },
            new FiscalPeriod { IdFiscalPeriod =  5, YearPeriod = 2026, MonthPeriod =  5, NamePeriod = "Mayo 2026",       StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  5,  1), EndDate = new DateOnly(2026,  5, 31) },
            new FiscalPeriod { IdFiscalPeriod =  6, YearPeriod = 2026, MonthPeriod =  6, NamePeriod = "Junio 2026",      StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  6,  1), EndDate = new DateOnly(2026,  6, 30) },
            new FiscalPeriod { IdFiscalPeriod =  7, YearPeriod = 2026, MonthPeriod =  7, NamePeriod = "Julio 2026",      StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  7,  1), EndDate = new DateOnly(2026,  7, 31) },
            new FiscalPeriod { IdFiscalPeriod =  8, YearPeriod = 2026, MonthPeriod =  8, NamePeriod = "Agosto 2026",     StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  8,  1), EndDate = new DateOnly(2026,  8, 31) },
            new FiscalPeriod { IdFiscalPeriod =  9, YearPeriod = 2026, MonthPeriod =  9, NamePeriod = "Septiembre 2026", StatusPeriod = "Abierto", StartDate = new DateOnly(2026,  9,  1), EndDate = new DateOnly(2026,  9, 30) },
            new FiscalPeriod { IdFiscalPeriod = 10, YearPeriod = 2026, MonthPeriod = 10, NamePeriod = "Octubre 2026",    StatusPeriod = "Abierto", StartDate = new DateOnly(2026, 10,  1), EndDate = new DateOnly(2026, 10, 31) },
            new FiscalPeriod { IdFiscalPeriod = 11, YearPeriod = 2026, MonthPeriod = 11, NamePeriod = "Noviembre 2026",  StatusPeriod = "Abierto", StartDate = new DateOnly(2026, 11,  1), EndDate = new DateOnly(2026, 11, 30) },
            new FiscalPeriod { IdFiscalPeriod = 12, YearPeriod = 2026, MonthPeriod = 12, NamePeriod = "Diciembre 2026",  StatusPeriod = "Abierto", StartDate = new DateOnly(2026, 12,  1), EndDate = new DateOnly(2026, 12, 31) }
        );
    }
}
