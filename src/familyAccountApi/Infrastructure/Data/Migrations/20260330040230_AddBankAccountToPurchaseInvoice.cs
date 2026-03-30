using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountToPurchaseInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idBankAccount",
                table: "purchaseInvoice",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idBankAccount",
                table: "purchaseInvoice",
                column: "idBankAccount");

            migrationBuilder.AddForeignKey(
                name: "FK_purchaseInvoice_bankAccount_idBankAccount",
                table: "purchaseInvoice",
                column: "idBankAccount",
                principalTable: "bankAccount",
                principalColumn: "idBankAccount",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchaseInvoice_bankAccount_idBankAccount",
                table: "purchaseInvoice");

            migrationBuilder.DropIndex(
                name: "IX_purchaseInvoice_idBankAccount",
                table: "purchaseInvoice");

            migrationBuilder.DropColumn(
                name: "idBankAccount",
                table: "purchaseInvoice");
        }
    }
}
