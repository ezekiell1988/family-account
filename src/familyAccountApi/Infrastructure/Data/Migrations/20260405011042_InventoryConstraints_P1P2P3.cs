using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventoryConstraints_P1P2P3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UQ_productUnit_idProduct_isBase",
                table: "productUnit",
                column: "idProduct",
                unique: true,
                filter: "[isBase] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_inventoryAdjustmentLine_unitCostNew",
                table: "inventoryAdjustmentLine",
                sql: "quantityDelta <= 0 OR unitCostNew IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_productUnit_idProduct_isBase",
                table: "productUnit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_inventoryAdjustmentLine_unitCostNew",
                table: "inventoryAdjustmentLine");
        }
    }
}
