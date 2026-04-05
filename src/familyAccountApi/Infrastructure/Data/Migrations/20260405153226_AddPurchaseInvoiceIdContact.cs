using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseInvoiceIdContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "providerName",
                table: "purchaseInvoice",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                comment: "Snapshot del nombre del proveedor en el momento de la factura. Se autocompleta desde el contacto si se envía IdContact.",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldComment: "Nombre del proveedor (ingreso libre, sin catálogo en esta fase).");

            migrationBuilder.AddColumn<int>(
                name: "idContact",
                table: "purchaseInvoice",
                type: "int",
                nullable: true,
                comment: "FK al contacto proveedor. Si es nulo, el proveedor no está en el catálogo.");

            migrationBuilder.InsertData(
                table: "contact",
                columns: new[] { "idContact", "codeContact", "name" },
                values: new object[] { 1, "SIN_PRO_CLI", "Sin proveedor / Cliente" });

            migrationBuilder.InsertData(
                table: "contactContactType",
                columns: new[] { "idContactContactType", "idContact", "idContactType" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 1, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idContact",
                table: "purchaseInvoice",
                column: "idContact");

            migrationBuilder.AddForeignKey(
                name: "FK_purchaseInvoice_contact_idContact",
                table: "purchaseInvoice",
                column: "idContact",
                principalTable: "contact",
                principalColumn: "idContact",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchaseInvoice_contact_idContact",
                table: "purchaseInvoice");

            migrationBuilder.DropIndex(
                name: "IX_purchaseInvoice_idContact",
                table: "purchaseInvoice");

            migrationBuilder.DeleteData(
                table: "contactContactType",
                keyColumn: "idContactContactType",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "contactContactType",
                keyColumn: "idContactContactType",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "contact",
                keyColumn: "idContact",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "idContact",
                table: "purchaseInvoice");

            migrationBuilder.AlterColumn<string>(
                name: "providerName",
                table: "purchaseInvoice",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                comment: "Nombre del proveedor (ingreso libre, sin catálogo en esta fase).",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldComment: "Snapshot del nombre del proveedor en el momento de la factura. Se autocompleta desde el contacto si se envía IdContact.");
        }
    }
}
