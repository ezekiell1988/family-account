using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCostCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idCostCenter",
                table: "accountingEntryLine",
                type: "int",
                nullable: true,
                comment: "FK opcional al centro de costo asociado a esta línea del asiento contable.");

            migrationBuilder.CreateTable(
                name: "costCenter",
                columns: table => new
                {
                    idCostCenter = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del centro de costo.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeCostCenter = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código único del centro de costo. Ejemplo: ADM, VTA, PROD."),
                    nameCostCenter = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre descriptivo del centro de costo. Ejemplo: Administración, Ventas."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si el centro de costo está activo y disponible para su uso en asientos contables.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_costCenter", x => x.idCostCenter);
                },
                comment: "Centros de costo para clasificar los asientos contables por área, proyecto o departamento.");

            migrationBuilder.CreateIndex(
                name: "IX_accountingEntryLine_idCostCenter",
                table: "accountingEntryLine",
                column: "idCostCenter",
                filter: "[idCostCenter] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_costCenter_codeCostCenter",
                table: "costCenter",
                column: "codeCostCenter",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_accountingEntryLine_costCenter_idCostCenter",
                table: "accountingEntryLine",
                column: "idCostCenter",
                principalTable: "costCenter",
                principalColumn: "idCostCenter",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accountingEntryLine_costCenter_idCostCenter",
                table: "accountingEntryLine");

            migrationBuilder.DropTable(
                name: "costCenter");

            migrationBuilder.DropIndex(
                name: "IX_accountingEntryLine_idCostCenter",
                table: "accountingEntryLine");

            migrationBuilder.DropColumn(
                name: "idCostCenter",
                table: "accountingEntryLine");
        }
    }
}
