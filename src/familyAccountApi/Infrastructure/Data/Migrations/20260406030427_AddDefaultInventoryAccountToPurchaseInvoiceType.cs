using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultInventoryAccountToPurchaseInvoiceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "idDefaultExpenseAccount",
                table: "purchaseInvoiceType",
                type: "int",
                nullable: true,
                comment: "FK a la cuenta contable de gasto alternativa. Solo se usa cuando el producto tiene un ProductAccount explícito que apunta a ella (override de cuenta de gasto en lugar de inventario).",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "FK a la cuenta contable de gasto usada como fallback cuando el SKU de la línea no tiene ProductAccount configurado. Permite confirmar facturas aunque los productos no tengan distribución contable.");

            migrationBuilder.AddColumn<int>(
                name: "idDefaultInventoryAccount",
                table: "purchaseInvoiceType",
                type: "int",
                nullable: true,
                comment: "FK a la cuenta contable de inventario (DR) usada por defecto al confirmar líneas con producto. Si el producto tiene ProductAccount configurado, esa cuenta de gasto tendrá prioridad.");

            migrationBuilder.UpdateData(
                table: "purchaseInvoiceType",
                keyColumn: "idPurchaseInvoiceType",
                keyValue: 1,
                column: "idDefaultInventoryAccount",
                value: 109);

            migrationBuilder.UpdateData(
                table: "purchaseInvoiceType",
                keyColumn: "idPurchaseInvoiceType",
                keyValue: 2,
                column: "idDefaultInventoryAccount",
                value: 109);

            migrationBuilder.UpdateData(
                table: "purchaseInvoiceType",
                keyColumn: "idPurchaseInvoiceType",
                keyValue: 3,
                column: "idDefaultInventoryAccount",
                value: 109);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceType_idDefaultInventoryAccount",
                table: "purchaseInvoiceType",
                column: "idDefaultInventoryAccount",
                filter: "[idDefaultInventoryAccount] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_purchaseInvoiceType_account_idDefaultInventoryAccount",
                table: "purchaseInvoiceType",
                column: "idDefaultInventoryAccount",
                principalTable: "account",
                principalColumn: "idAccount",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchaseInvoiceType_account_idDefaultInventoryAccount",
                table: "purchaseInvoiceType");

            migrationBuilder.DropIndex(
                name: "IX_purchaseInvoiceType_idDefaultInventoryAccount",
                table: "purchaseInvoiceType");

            migrationBuilder.DropColumn(
                name: "idDefaultInventoryAccount",
                table: "purchaseInvoiceType");

            migrationBuilder.AlterColumn<int>(
                name: "idDefaultExpenseAccount",
                table: "purchaseInvoiceType",
                type: "int",
                nullable: true,
                comment: "FK a la cuenta contable de gasto usada como fallback cuando el SKU de la línea no tiene ProductAccount configurado. Permite confirmar facturas aunque los productos no tengan distribución contable.",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "FK a la cuenta contable de gasto alternativa. Solo se usa cuando el producto tiene un ProductAccount explícito que apunta a ella (override de cuenta de gasto en lugar de inventario).");
        }
    }
}
