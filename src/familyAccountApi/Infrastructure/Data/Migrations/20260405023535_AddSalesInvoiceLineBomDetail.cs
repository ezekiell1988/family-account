using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoiceLineBomDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_salesInvoiceLine_lot_required",
                table: "salesInvoiceLine");

            migrationBuilder.AlterTable(
                name: "salesInvoiceLine",
                comment: "Línea de la factura de venta. IsNonProductLine=false + producto sin receta ni combo: IdInventoryLot obligatorio (lote directo). IsNonProductLine=false + producto con receta activa: BOM explosion en ConfirmAsync (BomDetails). IsNonProductLine=false + combo: explosión de slots en ConfirmAsync (BomDetails). IsNonProductLine=true: flete/servicio/gasto, sin movimiento de inventario.",
                oldComment: "Línea de la factura de venta. Cuando IsNonProductLine = false (línea de producto), IdInventoryLot es obligatorio y se descuenta al confirmar. Cuando IsNonProductLine = true (flete, servicio, gasto) IdInventoryLot puede ser NULL y no genera COGS.");

            migrationBuilder.AlterColumn<bool>(
                name: "isNonProductLine",
                table: "salesInvoiceLine",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "true = flete/servicio/gasto sin stock; false = producto. Cuando false y sin receta activa ni combo, idInventoryLot es obligatorio.",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false,
                oldComment: "true = línea de flete/servicio/gasto; false = línea de producto con stock. Cuando false, idInventoryLot es obligatorio.");

            migrationBuilder.AddColumn<int>(
                name: "idProductRecipe",
                table: "salesInvoiceLine",
                type: "int",
                nullable: true,
                comment: "Snapshot FK de la receta usada al confirmar (explosión BOM). NULL si el producto no tiene receta activa o es combo.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineBomDetail",
                columns: table => new
                {
                    idSalesInvoiceLineBomDetail = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del detalle BOM.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea de factura de venta que originó este movimiento."),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: true, comment: "FK nullable al slot del combo. NULL si la línea no es un combo."),
                    idProductRecipeLine = table.Column<int>(type: "int", nullable: true, comment: "FK nullable a la línea de receta. NULL si es reventa directa de slot o insumo extra."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Snapshot del insumo o producto de slot descontado al confirmar."),
                    idInventoryLot = table.Column<int>(type: "int", nullable: false, comment: "Lote específico del que se descontó el stock (FEFO auto-asignado)."),
                    quantityConsumed = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad descontada en unidad base del insumo/producto."),
                    unitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Snapshot del costo unitario del lote al momento de confirmar la factura.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineBomDetail", x => x.idSalesInvoiceLineBomDetail);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_productRecipeLine_idProductRecipeLine",
                        column: x => x.idProductRecipeLine,
                        principalTable: "productRecipeLine",
                        principalColumn: "idProductRecipeLine",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_salesInvoiceLine_idSalesInvoiceLine",
                        column: x => x.idSalesInvoiceLine,
                        principalTable: "salesInvoiceLine",
                        principalColumn: "idSalesInvoiceLine",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Detalle de movimiento de inventario generado al confirmar una SalesInvoiceLine mediante explosión BOM (receta activa — Opción 2B) o por slot de combo (Opción 3A). Una línea puede originar N registros: uno por insumo de receta o por producto de slot. IdProductRecipeLine = NULL indica reventa directa de slot o insumo extra no previsto en receta.");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idProductRecipe",
                table: "salesInvoiceLine",
                column: "idProductRecipe");

            migrationBuilder.AddCheckConstraint(
                name: "CK_salesInvoiceLine_lot_required",
                table: "salesInvoiceLine",
                sql: "isNonProductLine = 1 OR idInventoryLot IS NOT NULL OR idProductRecipe IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idInventoryLot",
                table: "salesInvoiceLineBomDetail",
                column: "idInventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idProduct",
                table: "salesInvoiceLineBomDetail",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idProductComboSlot",
                table: "salesInvoiceLineBomDetail",
                column: "idProductComboSlot",
                filter: "[idProductComboSlot] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idProductRecipeLine",
                table: "salesInvoiceLineBomDetail",
                column: "idProductRecipeLine",
                filter: "[idProductRecipeLine] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idSalesInvoiceLine",
                table: "salesInvoiceLineBomDetail",
                column: "idSalesInvoiceLine");

            migrationBuilder.AddForeignKey(
                name: "FK_salesInvoiceLine_productRecipe_idProductRecipe",
                table: "salesInvoiceLine",
                column: "idProductRecipe",
                principalTable: "productRecipe",
                principalColumn: "idProductRecipe",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_salesInvoiceLine_productRecipe_idProductRecipe",
                table: "salesInvoiceLine");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineBomDetail");

            migrationBuilder.DropIndex(
                name: "IX_salesInvoiceLine_idProductRecipe",
                table: "salesInvoiceLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_salesInvoiceLine_lot_required",
                table: "salesInvoiceLine");

            migrationBuilder.DropColumn(
                name: "idProductRecipe",
                table: "salesInvoiceLine");

            migrationBuilder.AlterTable(
                name: "salesInvoiceLine",
                comment: "Línea de la factura de venta. Cuando IsNonProductLine = false (línea de producto), IdInventoryLot es obligatorio y se descuenta al confirmar. Cuando IsNonProductLine = true (flete, servicio, gasto) IdInventoryLot puede ser NULL y no genera COGS.",
                oldComment: "Línea de la factura de venta. IsNonProductLine=false + producto sin receta ni combo: IdInventoryLot obligatorio (lote directo). IsNonProductLine=false + producto con receta activa: BOM explosion en ConfirmAsync (BomDetails). IsNonProductLine=false + combo: explosión de slots en ConfirmAsync (BomDetails). IsNonProductLine=true: flete/servicio/gasto, sin movimiento de inventario.");

            migrationBuilder.AlterColumn<bool>(
                name: "isNonProductLine",
                table: "salesInvoiceLine",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "true = línea de flete/servicio/gasto; false = línea de producto con stock. Cuando false, idInventoryLot es obligatorio.",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false,
                oldComment: "true = flete/servicio/gasto sin stock; false = producto. Cuando false y sin receta activa ni combo, idInventoryLot es obligatorio.");

            migrationBuilder.AddCheckConstraint(
                name: "CK_salesInvoiceLine_lot_required",
                table: "salesInvoiceLine",
                sql: "isNonProductLine = 1 OR idInventoryLot IS NOT NULL");
        }
    }
}
