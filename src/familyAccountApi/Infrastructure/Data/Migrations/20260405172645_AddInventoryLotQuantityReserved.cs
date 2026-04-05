using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLotQuantityReserved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "inventoryLot",
                comment: "Registro de stock de inventario por producto y lote. Es la unidad mínima de trazabilidad. quantityAvailable nunca se edita directamente: solo se modifica al confirmar purchaseInvoice, salesInvoice o inventoryAdjustment. quantityReserved se incrementa al asignar un SalesOrderLineFulfillment tipo Stock y se decrementa al confirmar o eliminar el fulfillment.",
                oldComment: "Registro de stock de inventario por producto y lote. Es la unidad mínima de trazabilidad. quantityAvailable nunca se edita directamente: solo se modifica al confirmar purchaseInvoice, salesInvoice o inventoryAdjustment.");

            migrationBuilder.AddColumn<decimal>(
                name: "quantityReserved",
                table: "inventoryLot",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                comment: "Stock reservado por SalesOrderLineFulfillment de tipo Stock pendientes de confirmar. Se incrementa al asignar un fulfillment y se decrementa al confirmar o eliminar el fulfillment. QuantityAvailableNet = QuantityAvailable - QuantityReserved.");

            migrationBuilder.AddCheckConstraint(
                name: "CK_inventoryLot_quantityReserved",
                table: "inventoryLot",
                sql: "quantityReserved >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_inventoryLot_quantityReserved",
                table: "inventoryLot");

            migrationBuilder.DropColumn(
                name: "quantityReserved",
                table: "inventoryLot");

            migrationBuilder.AlterTable(
                name: "inventoryLot",
                comment: "Registro de stock de inventario por producto y lote. Es la unidad mínima de trazabilidad. quantityAvailable nunca se edita directamente: solo se modifica al confirmar purchaseInvoice, salesInvoice o inventoryAdjustment.",
                oldComment: "Registro de stock de inventario por producto y lote. Es la unidad mínima de trazabilidad. quantityAvailable nunca se edita directamente: solo se modifica al confirmar purchaseInvoice, salesInvoice o inventoryAdjustment. quantityReserved se incrementa al asignar un SalesOrderLineFulfillment tipo Stock y se decrementa al confirmar o eliminar el fulfillment.");
        }
    }
}
