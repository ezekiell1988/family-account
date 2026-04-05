using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddC5Options : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idProductRecipe",
                table: "productOptionItem",
                type: "int",
                nullable: true,
                comment: "FK opcional a la receta que se usa para producir este option item (ej: receta de masa delgada).");

            migrationBuilder.CreateTable(
                name: "productOptionItemAvailability",
                columns: table => new
                {
                    idProductOptionItemAvailability = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idRestrictedItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem restringido."),
                    idEnablingItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem que habilita al restringido.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productOptionItemAvailability", x => x.idProductOptionItemAvailability);
                    table.ForeignKey(
                        name: "FK_productOptionItemAvailability_productOptionItem_idEnablingItem",
                        column: x => x.idEnablingItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productOptionItemAvailability_productOptionItem_idRestrictedItem",
                        column: x => x.idRestrictedItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Reglas de disponibilidad condicional entre items de opción. El item restringido (idRestrictedItem) solo está disponible cuando al menos uno de sus ítems habilitadores (idEnablingItem) está seleccionado en el pedido.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineOption",
                columns: table => new
                {
                    idSalesInvoiceLineOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea de la factura."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este option item aplicado a la línea.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineOption", x => x.idSalesInvoiceLineOption);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineOption_salesInvoiceLine_idSalesInvoiceLine",
                        column: x => x.idSalesInvoiceLine,
                        principalTable: "salesInvoiceLine",
                        principalColumn: "idSalesInvoiceLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opciones configurables copiadas desde el pedido a la factura de venta.");

            migrationBuilder.CreateTable(
                name: "salesOrderLineOption",
                columns: table => new
                {
                    idSalesOrderLineOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea del pedido."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción seleccionado."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este option item aplicado a la línea (por defecto 1).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLineOption", x => x.idSalesOrderLineOption);
                    table.ForeignKey(
                        name: "FK_salesOrderLineOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineOption_salesOrderLine_idSalesOrderLine",
                        column: x => x.idSalesOrderLine,
                        principalTable: "salesOrderLine",
                        principalColumn: "idSalesOrderLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opciones configurables seleccionadas en una línea de pedido (ej: masa delgada, extra queso).");

            migrationBuilder.CreateIndex(
                name: "IX_productOptionItem_idProductRecipe",
                table: "productOptionItem",
                column: "idProductRecipe");

            migrationBuilder.CreateIndex(
                name: "IX_productOptionItemAvailability_idEnablingItem",
                table: "productOptionItemAvailability",
                column: "idEnablingItem");

            migrationBuilder.CreateIndex(
                name: "UQ_productOptionItemAvailability_idRestrictedItem_idEnablingItem",
                table: "productOptionItemAvailability",
                columns: new[] { "idRestrictedItem", "idEnablingItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineOption_idProductOptionItem",
                table: "salesInvoiceLineOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineOption_idSalesInvoiceLine",
                table: "salesInvoiceLineOption",
                column: "idSalesInvoiceLine");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineOption_idProductOptionItem",
                table: "salesOrderLineOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineOption_idSalesOrderLine",
                table: "salesOrderLineOption",
                column: "idSalesOrderLine");

            migrationBuilder.AddForeignKey(
                name: "FK_productOptionItem_productRecipe_idProductRecipe",
                table: "productOptionItem",
                column: "idProductRecipe",
                principalTable: "productRecipe",
                principalColumn: "idProductRecipe",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_productOptionItem_productRecipe_idProductRecipe",
                table: "productOptionItem");

            migrationBuilder.DropTable(
                name: "productOptionItemAvailability");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineOption");

            migrationBuilder.DropTable(
                name: "salesOrderLineOption");

            migrationBuilder.DropIndex(
                name: "IX_productOptionItem_idProductRecipe",
                table: "productOptionItem");

            migrationBuilder.DropColumn(
                name: "idProductRecipe",
                table: "productOptionItem");
        }
    }
}
