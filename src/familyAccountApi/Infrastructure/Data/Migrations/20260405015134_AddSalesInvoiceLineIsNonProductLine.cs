using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoiceLineIsNonProductLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "salesInvoiceLine",
                comment: "Línea de la factura de venta. Cuando IsNonProductLine = false (línea de producto), IdInventoryLot es obligatorio y se descuenta al confirmar. Cuando IsNonProductLine = true (flete, servicio, gasto) IdInventoryLot puede ser NULL y no genera COGS.",
                oldComment: "Línea de la factura de venta. IdInventoryLot es obligatorio para productos con stock; se descuenta al confirmar.");

            migrationBuilder.AddColumn<bool>(
                name: "isNonProductLine",
                table: "salesInvoiceLine",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "true = línea de flete/servicio/gasto; false = línea de producto con stock. Cuando false, idInventoryLot es obligatorio.");

            migrationBuilder.AddCheckConstraint(
                name: "CK_salesInvoiceLine_lot_required",
                table: "salesInvoiceLine",
                sql: "isNonProductLine = 1 OR idInventoryLot IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_salesInvoiceLine_lot_required",
                table: "salesInvoiceLine");

            migrationBuilder.DropColumn(
                name: "isNonProductLine",
                table: "salesInvoiceLine");

            migrationBuilder.AlterTable(
                name: "salesInvoiceLine",
                comment: "Línea de la factura de venta. IdInventoryLot es obligatorio para productos con stock; se descuenta al confirmar.",
                oldComment: "Línea de la factura de venta. Cuando IsNonProductLine = false (línea de producto), IdInventoryLot es obligatorio y se descuenta al confirmar. Cuando IsNonProductLine = true (flete, servicio, gasto) IdInventoryLot puede ser NULL y no genera COGS.");
        }
    }
}
