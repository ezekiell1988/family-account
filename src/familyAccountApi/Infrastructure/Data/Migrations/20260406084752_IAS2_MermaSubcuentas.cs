using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IAS2_MermaSubcuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 113,
                column: "allowsMovements",
                value: false);

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[,]
                {
                    { 129, true, "5.14.01.01", 113, true, 4, "Merma Normal", "Gasto" },
                    { 130, true, "5.14.01.02", 113, true, 4, "Merma Anormal", "Gasto" }
                });

            migrationBuilder.UpdateData(
                table: "inventoryAdjustmentType",
                keyColumn: "idInventoryAdjustmentType",
                keyValue: 1,
                column: "idAccountCounterpartExit",
                value: 130);

            migrationBuilder.UpdateData(
                table: "inventoryAdjustmentType",
                keyColumn: "idInventoryAdjustmentType",
                keyValue: 3,
                column: "idAccountCounterpartExit",
                value: 130);

            migrationBuilder.InsertData(
                table: "inventoryAdjustmentType",
                columns: new[] { "idInventoryAdjustmentType", "codeInventoryAdjustmentType", "idAccountCounterpartEntry", "idAccountCounterpartExit", "idAccountInventoryDefault", "isActive", "nameInventoryAdjustmentType" },
                values: new object[] { 4, "REGALIA", null, 130, 109, true, "Regalía" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "inventoryAdjustmentType",
                keyColumn: "idInventoryAdjustmentType",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 130);

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 113,
                column: "allowsMovements",
                value: true);

            migrationBuilder.UpdateData(
                table: "inventoryAdjustmentType",
                keyColumn: "idInventoryAdjustmentType",
                keyValue: 1,
                column: "idAccountCounterpartExit",
                value: 113);

            migrationBuilder.UpdateData(
                table: "inventoryAdjustmentType",
                keyColumn: "idInventoryAdjustmentType",
                keyValue: 3,
                column: "idAccountCounterpartExit",
                value: 113);
        }
    }
}
