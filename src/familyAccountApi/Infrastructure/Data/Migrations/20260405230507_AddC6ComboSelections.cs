using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddC6ComboSelections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "productComboSlotPresetOption",
                columns: table => new
                {
                    idProductComboSlotPresetOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "FK al slot del combo al que pertenece esta opción preset."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción preseleccionado (debe pertenecer al producto del slot).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productComboSlotPresetOption", x => x.idProductComboSlotPresetOption);
                    table.ForeignKey(
                        name: "FK_productComboSlotPresetOption_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productComboSlotPresetOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Opciones preseleccionadas en el catálogo para un slot de combo. El cliente las ve bloqueadas (no editables).");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineComboSlotSelection",
                columns: table => new
                {
                    idSalesInvoiceLineComboSlotSelection = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea de la factura (combo)."),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "FK al slot del combo."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Producto elegido en el slot (snapshot al momento de facturar)."),
                    idInventoryLot = table.Column<int>(type: "int", nullable: true, comment: "Lote de producto terminado pre-asignado desde producción (nullable — slot sin receta lo omite).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineComboSlotSelection", x => x.idSalesInvoiceLineComboSlotSelection);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineComboSlotSelection_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineComboSlotSelection_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineComboSlotSelection_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineComboSlotSelection_salesInvoiceLine_idSalesInvoiceLine",
                        column: x => x.idSalesInvoiceLine,
                        principalTable: "salesInvoiceLine",
                        principalColumn: "idSalesInvoiceLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Snapshot inmutable de la selección por slot al generar la factura de venta.");

            migrationBuilder.CreateTable(
                name: "salesOrderLineComboSlotSelection",
                columns: table => new
                {
                    idSalesOrderLineComboSlotSelection = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea del pedido (combo)."),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "FK al slot del combo configurado."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Producto elegido por el cliente en este slot.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLineComboSlotSelection", x => x.idSalesOrderLineComboSlotSelection);
                    table.ForeignKey(
                        name: "FK_salesOrderLineComboSlotSelection_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineComboSlotSelection_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineComboSlotSelection_salesOrderLine_idSalesOrderLine",
                        column: x => x.idSalesOrderLine,
                        principalTable: "salesOrderLine",
                        principalColumn: "idSalesOrderLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Selección del cliente para cada slot del combo en una línea de pedido.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineSlotOption",
                columns: table => new
                {
                    idSalesInvoiceLineSlotOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLineComboSlotSelection = table.Column<int>(type: "int", nullable: false, comment: "FK a la selección de slot de la línea de factura."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción del slot."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este option item aplicado al slot.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineSlotOption", x => x.idSalesInvoiceLineSlotOption);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineSlotOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineSlotOption_salesInvoiceLineComboSlotSelection_idSalesInvoiceLineComboSlotSelection",
                        column: x => x.idSalesInvoiceLineComboSlotSelection,
                        principalTable: "salesInvoiceLineComboSlotSelection",
                        principalColumn: "idSalesInvoiceLineComboSlotSelection",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opciones del slot incluidas en la factura (snapshot al copiar desde el pedido).");

            migrationBuilder.CreateTable(
                name: "salesOrderLineSlotOption",
                columns: table => new
                {
                    idSalesOrderLineSlotOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrderLineComboSlotSelection = table.Column<int>(type: "int", nullable: false, comment: "FK a la selección de slot de la línea del pedido."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción elegido dentro del slot."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este option item aplicado al slot (por defecto 1).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLineSlotOption", x => x.idSalesOrderLineSlotOption);
                    table.ForeignKey(
                        name: "FK_salesOrderLineSlotOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineSlotOption_salesOrderLineComboSlotSelection_idSalesOrderLineComboSlotSelection",
                        column: x => x.idSalesOrderLineComboSlotSelection,
                        principalTable: "salesOrderLineComboSlotSelection",
                        principalColumn: "idSalesOrderLineComboSlotSelection",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opciones elegidas dentro de cada selección de slot (incluye presets copiados y opciones libres del cliente).");

            migrationBuilder.CreateIndex(
                name: "IX_productComboSlotPresetOption_idProductOptionItem",
                table: "productComboSlotPresetOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "UQ_productComboSlotPresetOption_slot_item",
                table: "productComboSlotPresetOption",
                columns: new[] { "idProductComboSlot", "idProductOptionItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineComboSlotSelection_idInventoryLot",
                table: "salesInvoiceLineComboSlotSelection",
                column: "idInventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineComboSlotSelection_idProduct",
                table: "salesInvoiceLineComboSlotSelection",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineComboSlotSelection_idProductComboSlot",
                table: "salesInvoiceLineComboSlotSelection",
                column: "idProductComboSlot");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceLineComboSlotSelection_line_slot",
                table: "salesInvoiceLineComboSlotSelection",
                columns: new[] { "idSalesInvoiceLine", "idProductComboSlot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineSlotOption_idProductOptionItem",
                table: "salesInvoiceLineSlotOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceLineSlotOption_selection_item",
                table: "salesInvoiceLineSlotOption",
                columns: new[] { "idSalesInvoiceLineComboSlotSelection", "idProductOptionItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineComboSlotSelection_idProduct",
                table: "salesOrderLineComboSlotSelection",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineComboSlotSelection_idProductComboSlot",
                table: "salesOrderLineComboSlotSelection",
                column: "idProductComboSlot");

            migrationBuilder.CreateIndex(
                name: "UQ_salesOrderLineComboSlotSelection_line_slot",
                table: "salesOrderLineComboSlotSelection",
                columns: new[] { "idSalesOrderLine", "idProductComboSlot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineSlotOption_idProductOptionItem",
                table: "salesOrderLineSlotOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "UQ_salesOrderLineSlotOption_selection_item",
                table: "salesOrderLineSlotOption",
                columns: new[] { "idSalesOrderLineComboSlotSelection", "idProductOptionItem" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "productComboSlotPresetOption");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineSlotOption");

            migrationBuilder.DropTable(
                name: "salesOrderLineSlotOption");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineComboSlotSelection");

            migrationBuilder.DropTable(
                name: "salesOrderLineComboSlotSelection");
        }
    }
}
