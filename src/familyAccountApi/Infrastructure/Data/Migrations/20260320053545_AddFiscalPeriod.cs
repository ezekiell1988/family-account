using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiscalPeriod",
                columns: table => new
                {
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del período fiscal.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    yearPeriod = table.Column<int>(type: "int", nullable: false, comment: "Año calendario del período fiscal (p. ej. 2026)."),
                    monthPeriod = table.Column<int>(type: "int", nullable: false, comment: "Número de mes del período fiscal: 1=Enero, ..., 12=Diciembre."),
                    namePeriod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nombre descriptivo del período (p. ej. 'Enero 2026')."),
                    statusPeriod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Estado del período: 'Abierto' permite movimientos, 'Cerrado' no admite nuevos registros, 'Bloqueado' está bloqueado administrativamente."),
                    startDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de inicio del período fiscal (primer día del mes)."),
                    endDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de fin del período fiscal (último día del mes).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscalPeriod", x => x.idFiscalPeriod);
                    table.CheckConstraint("CK_fiscalPeriod_statusPeriod", "statusPeriod IN ('Abierto', 'Cerrado', 'Bloqueado')");
                },
                comment: "Períodos fiscales del sistema contable. Cada período representa un mes de un año y controla si se permiten movimientos contables en ese período.");

            migrationBuilder.InsertData(
                table: "fiscalPeriod",
                columns: new[] { "idFiscalPeriod", "endDate", "monthPeriod", "namePeriod", "startDate", "statusPeriod", "yearPeriod" },
                values: new object[,]
                {
                    { 1, new DateOnly(2026, 1, 31), 1, "Enero 2026", new DateOnly(2026, 1, 1), "Abierto", 2026 },
                    { 2, new DateOnly(2026, 2, 28), 2, "Febrero 2026", new DateOnly(2026, 2, 1), "Abierto", 2026 },
                    { 3, new DateOnly(2026, 3, 31), 3, "Marzo 2026", new DateOnly(2026, 3, 1), "Abierto", 2026 },
                    { 4, new DateOnly(2026, 4, 30), 4, "Abril 2026", new DateOnly(2026, 4, 1), "Abierto", 2026 },
                    { 5, new DateOnly(2026, 5, 31), 5, "Mayo 2026", new DateOnly(2026, 5, 1), "Abierto", 2026 },
                    { 6, new DateOnly(2026, 6, 30), 6, "Junio 2026", new DateOnly(2026, 6, 1), "Abierto", 2026 },
                    { 7, new DateOnly(2026, 7, 31), 7, "Julio 2026", new DateOnly(2026, 7, 1), "Abierto", 2026 },
                    { 8, new DateOnly(2026, 8, 31), 8, "Agosto 2026", new DateOnly(2026, 8, 1), "Abierto", 2026 },
                    { 9, new DateOnly(2026, 9, 30), 9, "Septiembre 2026", new DateOnly(2026, 9, 1), "Abierto", 2026 },
                    { 10, new DateOnly(2026, 10, 31), 10, "Octubre 2026", new DateOnly(2026, 10, 1), "Abierto", 2026 },
                    { 11, new DateOnly(2026, 11, 30), 11, "Noviembre 2026", new DateOnly(2026, 11, 1), "Abierto", 2026 },
                    { 12, new DateOnly(2026, 12, 31), 12, "Diciembre 2026", new DateOnly(2026, 12, 1), "Abierto", 2026 }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_fiscalPeriod_yearPeriod_monthPeriod",
                table: "fiscalPeriod",
                columns: new[] { "yearPeriod", "monthPeriod" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiscalPeriod");
        }
    }
}
