using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseInvoiceTypeBankMovementType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idBankMovementType",
                table: "purchaseInvoiceType",
                type: "int",
                nullable: true,
                comment: "FK al tipo de movimiento bancario usado para auto-crear el BankMovement al confirmar. Solo aplica cuando CounterpartFromBankMovement = true (DEBITO, TC).");

            migrationBuilder.UpdateData(
                table: "purchaseInvoiceType",
                keyColumn: "idPurchaseInvoiceType",
                keyValue: 1,
                column: "idBankMovementType",
                value: null);

            migrationBuilder.UpdateData(
                table: "purchaseInvoiceType",
                keyColumn: "idPurchaseInvoiceType",
                keyValue: 2,
                column: "idBankMovementType",
                value: 4);

            migrationBuilder.UpdateData(
                table: "purchaseInvoiceType",
                keyColumn: "idPurchaseInvoiceType",
                keyValue: 3,
                column: "idBankMovementType",
                value: 6);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceType_idBankMovementType",
                table: "purchaseInvoiceType",
                column: "idBankMovementType",
                filter: "[idBankMovementType] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_purchaseInvoiceType_bankMovementType_idBankMovementType",
                table: "purchaseInvoiceType",
                column: "idBankMovementType",
                principalTable: "bankMovementType",
                principalColumn: "idBankMovementType",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchaseInvoiceType_bankMovementType_idBankMovementType",
                table: "purchaseInvoiceType");

            migrationBuilder.DropIndex(
                name: "IX_purchaseInvoiceType_idBankMovementType",
                table: "purchaseInvoiceType");

            migrationBuilder.DropColumn(
                name: "idBankMovementType",
                table: "purchaseInvoiceType");
        }
    }
}
