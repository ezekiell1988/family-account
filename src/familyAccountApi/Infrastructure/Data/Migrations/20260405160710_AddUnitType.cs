using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_unitOfMeasure_typeUnit",
                table: "unitOfMeasure");

            migrationBuilder.DropColumn(
                name: "typeUnit",
                table: "unitOfMeasure");

            migrationBuilder.AddColumn<int>(
                name: "idUnitType",
                table: "unitOfMeasure",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "unitType",
                columns: table => new
                {
                    idUnitType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de unidad.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nameUnitType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, comment: "Nombre del tipo dimensional: Unidad | Volumen | Masa | Longitud.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unitType", x => x.idUnitType);
                },
                comment: "Clasificación dimensional de unidades de medida. Catálogo de sistema, sin CRUD expuesto al usuario.");

            migrationBuilder.UpdateData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 1,
                column: "idUnitType",
                value: 1);

            migrationBuilder.UpdateData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 2,
                column: "idUnitType",
                value: 2);

            migrationBuilder.UpdateData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 3,
                column: "idUnitType",
                value: 3);

            migrationBuilder.UpdateData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 4,
                column: "idUnitType",
                value: 4);

            migrationBuilder.InsertData(
                table: "unitType",
                columns: new[] { "idUnitType", "nameUnitType" },
                values: new object[,]
                {
                    { 1, "Unidad" },
                    { 2, "Volumen" },
                    { 3, "Masa" },
                    { 4, "Longitud" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_unitOfMeasure_idUnitType",
                table: "unitOfMeasure",
                column: "idUnitType");

            migrationBuilder.CreateIndex(
                name: "UQ_unitType_nameUnitType",
                table: "unitType",
                column: "nameUnitType",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_unitOfMeasure_unitType",
                table: "unitOfMeasure",
                column: "idUnitType",
                principalTable: "unitType",
                principalColumn: "idUnitType",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_unitOfMeasure_unitType",
                table: "unitOfMeasure");

            migrationBuilder.DropTable(
                name: "unitType");

            migrationBuilder.DropIndex(
                name: "IX_unitOfMeasure_idUnitType",
                table: "unitOfMeasure");

            migrationBuilder.DropColumn(
                name: "idUnitType",
                table: "unitOfMeasure");

            migrationBuilder.AddColumn<string>(
                name: "typeUnit",
                table: "unitOfMeasure",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                comment: "Clasificación dimensional: Volumen | Masa | Unidad | Longitud.");

            migrationBuilder.UpdateData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 1,
                column: "typeUnit",
                value: "Unidad");

            migrationBuilder.UpdateData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 2,
                column: "typeUnit",
                value: "Volumen");

            migrationBuilder.UpdateData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 3,
                column: "typeUnit",
                value: "Masa");

            migrationBuilder.UpdateData(
                table: "unitOfMeasure",
                keyColumn: "idUnit",
                keyValue: 4,
                column: "typeUnit",
                value: "Longitud");

            migrationBuilder.AddCheckConstraint(
                name: "CK_unitOfMeasure_typeUnit",
                table: "unitOfMeasure",
                sql: "typeUnit IN ('Volumen', 'Masa', 'Unidad', 'Longitud')");
        }
    }
}
