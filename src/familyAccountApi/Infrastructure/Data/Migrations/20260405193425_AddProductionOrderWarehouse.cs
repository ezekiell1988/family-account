using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOrderWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idWarehouse",
                table: "productionOrder",
                type: "int",
                nullable: true,
                comment: "Bodega de producción: consumo de materias primas y entrada del producto terminado.");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrder_idWarehouse",
                table: "productionOrder",
                column: "idWarehouse",
                filter: "[idWarehouse] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_productionOrder_warehouse_idWarehouse",
                table: "productionOrder",
                column: "idWarehouse",
                principalTable: "warehouse",
                principalColumn: "idWarehouse",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_productionOrder_warehouse_idWarehouse",
                table: "productionOrder");

            migrationBuilder.DropIndex(
                name: "IX_productionOrder_idWarehouse",
                table: "productionOrder");

            migrationBuilder.DropColumn(
                name: "idWarehouse",
                table: "productionOrder");
        }
    }
}
