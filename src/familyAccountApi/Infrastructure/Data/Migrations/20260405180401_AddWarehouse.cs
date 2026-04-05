using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idWarehouse",
                table: "purchaseInvoice",
                type: "int",
                nullable: true,
                comment: "FK al almacén destino de la mercadería. Opcional; si es nulo al confirmar se usa el almacén predeterminado.");

            migrationBuilder.AddColumn<int>(
                name: "idWarehouse",
                table: "inventoryLot",
                type: "int",
                nullable: false,
                defaultValue: 1,
                comment: "FK al almacén donde se encuentra este lote. Por defecto el almacén Principal (id=1).");

            migrationBuilder.CreateTable(
                name: "warehouse",
                columns: table => new
                {
                    idWarehouse = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del almacén.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nameWarehouse = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nombre descriptivo del almacén. Debe ser único."),
                    isDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Indica si este es el almacén predeterminado. Solo uno puede ser predeterminado a la vez. Se usa cuando no se especifica almacén al ingresar mercadería."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Almacén activo. Los almacenes inactivos no aceptan nuevas entradas de stock.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse", x => x.idWarehouse);
                },
                comment: "Almacenes o bodegas de la empresa. El stock de inventario se segmenta por almacén a través de InventoryLot.idWarehouse.");

            migrationBuilder.InsertData(
                table: "warehouse",
                columns: new[] { "idWarehouse", "isActive", "isDefault", "nameWarehouse" },
                values: new object[] { 1, true, true, "Principal" });

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idWarehouse",
                table: "purchaseInvoice",
                column: "idWarehouse",
                filter: "[idWarehouse] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryLot_idWarehouse",
                table: "inventoryLot",
                column: "idWarehouse");

            migrationBuilder.CreateIndex(
                name: "UQ_warehouse_nameWarehouse",
                table: "warehouse",
                column: "nameWarehouse",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_inventoryLot_warehouse_idWarehouse",
                table: "inventoryLot",
                column: "idWarehouse",
                principalTable: "warehouse",
                principalColumn: "idWarehouse",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchaseInvoice_warehouse_idWarehouse",
                table: "purchaseInvoice",
                column: "idWarehouse",
                principalTable: "warehouse",
                principalColumn: "idWarehouse",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventoryLot_warehouse_idWarehouse",
                table: "inventoryLot");

            migrationBuilder.DropForeignKey(
                name: "FK_purchaseInvoice_warehouse_idWarehouse",
                table: "purchaseInvoice");

            migrationBuilder.DropTable(
                name: "warehouse");

            migrationBuilder.DropIndex(
                name: "IX_purchaseInvoice_idWarehouse",
                table: "purchaseInvoice");

            migrationBuilder.DropIndex(
                name: "IX_inventoryLot_idWarehouse",
                table: "inventoryLot");

            migrationBuilder.DropColumn(
                name: "idWarehouse",
                table: "purchaseInvoice");

            migrationBuilder.DropColumn(
                name: "idWarehouse",
                table: "inventoryLot");
        }
    }
}
