using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLotIdInventoryAdjustmentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventoryLot_idInventoryAdjustment",
                table: "inventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryLot_idInventoryAdjustment",
                table: "inventoryLot",
                column: "idInventoryAdjustment",
                filter: "[idInventoryAdjustment] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventoryLot_idInventoryAdjustment",
                table: "inventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryLot_idInventoryAdjustment",
                table: "inventoryLot",
                column: "idInventoryAdjustment");
        }
    }
}
