using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModuloVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_bankMovementDocument_typeDocument",
                table: "bankMovementDocument");

            migrationBuilder.AlterColumn<string>(
                name: "typeDocument",
                table: "bankMovementDocument",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                comment: "Tipo de documento: 'FacturaCompra', 'FacturaVenta', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldComment: "Tipo de documento: 'FacturaCompra', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'");

            migrationBuilder.AddColumn<int>(
                name: "idSalesInvoice",
                table: "bankMovementDocument",
                type: "int",
                nullable: true,
                comment: "FK opcional a la factura de venta vinculada a este documento de soporte");

            migrationBuilder.CreateTable(
                name: "salesInvoiceType",
                columns: table => new
                {
                    idSalesInvoiceType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de factura de venta.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeSalesInvoiceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Código único del tipo: 'CONTADO_CRC', 'CONTADO_USD', 'CREDITO'."),
                    nameSalesInvoiceType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre descriptivo del tipo de factura de venta."),
                    counterpartFromBankMovement = table.Column<bool>(type: "bit", nullable: false, comment: "true = la cuenta DR del asiento proviene del BankMovement vinculado; false = cuenta Caja fija por moneda."),
                    idAccountCounterpartCRC = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta Caja CRC. Solo aplica cuando CounterpartFromBankMovement = false."),
                    idAccountCounterpartUSD = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta Caja USD. Solo aplica cuando CounterpartFromBankMovement = false."),
                    idBankMovementType = table.Column<int>(type: "int", nullable: true, comment: "FK al tipo de movimiento bancario para auto-crear el BankMovement al confirmar. Solo si CounterpartFromBankMovement = true."),
                    idAccountSalesRevenue = table.Column<int>(type: "int", nullable: true, comment: "Cuenta CR de ingresos por ventas (fallback cuando el producto no tiene ProductAccount configurado)."),
                    idAccountCOGS = table.Column<int>(type: "int", nullable: true, comment: "Cuenta DR de costo de ventas (COGS) al reconocer el costo."),
                    idAccountInventory = table.Column<int>(type: "int", nullable: true, comment: "Cuenta CR de inventario al reconocer el costo de ventas."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si el tipo de factura de venta está activo.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceType", x => x.idSalesInvoiceType);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountCOGS",
                        column: x => x.idAccountCOGS,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountCounterpartCRC",
                        column: x => x.idAccountCounterpartCRC,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountCounterpartUSD",
                        column: x => x.idAccountCounterpartUSD,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountInventory",
                        column: x => x.idAccountInventory,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountSalesRevenue",
                        column: x => x.idAccountSalesRevenue,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_bankMovementType_idBankMovementType",
                        column: x => x.idBankMovementType,
                        principalTable: "bankMovementType",
                        principalColumn: "idBankMovementType",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo de tipos de factura de venta. Define contrapartida (Caja o BankMovement) y cuentas contables predeterminadas para ingresos y COGS.");

            migrationBuilder.CreateTable(
                name: "salesInvoice",
                columns: table => new
                {
                    idSalesInvoice = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la factura de venta.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false),
                    idCurrency = table.Column<int>(type: "int", nullable: false),
                    idSalesInvoiceType = table.Column<int>(type: "int", nullable: false),
                    idContact = table.Column<int>(type: "int", nullable: true),
                    idBankAccount = table.Column<int>(type: "int", nullable: true),
                    numberInvoice = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número correlativo: FV-YYYYMMDD-NNN (asignado al confirmar; en Borrador = 'BORRADOR')."),
                    dateInvoice = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del documento de venta."),
                    subTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Subtotal sin impuesto."),
                    taxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto total de impuesto."),
                    totalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total del documento (subtotal + impuesto)."),
                    statusInvoice = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, defaultValue: "Borrador", comment: "Estado: Borrador | Confirmado | Anulado."),
                    descriptionInvoice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Observaciones opcionales del documento."),
                    exchangeRateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Tipo de cambio vigente al momento de la venta."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoice", x => x.idSalesInvoice);
                    table.CheckConstraint("CK_salesInvoice_statusInvoice", "statusInvoice IN ('Borrador', 'Confirmado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_salesInvoice_bankAccount_idBankAccount",
                        column: x => x.idBankAccount,
                        principalTable: "bankAccount",
                        principalColumn: "idBankAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_contact_idContact",
                        column: x => x.idContact,
                        principalTable: "contact",
                        principalColumn: "idContact",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_salesInvoiceType_idSalesInvoiceType",
                        column: x => x.idSalesInvoiceType,
                        principalTable: "salesInvoiceType",
                        principalColumn: "idSalesInvoiceType",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Cabecera de la factura de venta. Flujo: Borrador → Confirmado (genera asiento + COGS + descuenta lote) → Anulado (revierte).");

            migrationBuilder.CreateTable(
                name: "salesInvoiceEntry",
                columns: table => new
                {
                    idSalesInvoiceEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoice = table.Column<int>(type: "int", nullable: false),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceEntry", x => x.idSalesInvoiceEntry);
                    table.ForeignKey(
                        name: "FK_salesInvoiceEntry_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceEntry_salesInvoice_idSalesInvoice",
                        column: x => x.idSalesInvoice,
                        principalTable: "salesInvoice",
                        principalColumn: "idSalesInvoice",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla pivot N:M salesInvoice ↔ accountingEntry.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLine",
                columns: table => new
                {
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoice = table.Column<int>(type: "int", nullable: false),
                    idProduct = table.Column<int>(type: "int", nullable: true),
                    idUnit = table.Column<int>(type: "int", nullable: true),
                    idInventoryLot = table.Column<int>(type: "int", nullable: true),
                    descriptionLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false, comment: "Descripción del producto o servicio de la línea."),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad en la unidad de venta seleccionada."),
                    quantityBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true, comment: "Cantidad en unidad base, calculada al confirmar: Quantity × ConversionFactor."),
                    unitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Precio unitario de venta en la presentación seleccionada."),
                    unitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true, comment: "Costo unitario snapshot del lote en el momento de confirmar (para calcular COGS)."),
                    taxPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "Porcentaje de impuesto aplicado a esta línea."),
                    totalLineAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Total de la línea incluyendo impuesto.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLine", x => x.idSalesInvoiceLine);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_salesInvoice_idSalesInvoice",
                        column: x => x.idSalesInvoice,
                        principalTable: "salesInvoice",
                        principalColumn: "idSalesInvoice",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_unitOfMeasure_idUnit",
                        column: x => x.idUnit,
                        principalTable: "unitOfMeasure",
                        principalColumn: "idUnit",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Línea de la factura de venta. IdInventoryLot es obligatorio para productos con stock; se descuenta al confirmar.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineEntry",
                columns: table => new
                {
                    idSalesInvoiceLineEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false),
                    idAccountingEntryLine = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineEntry", x => x.idSalesInvoiceLineEntry);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineEntry_accountingEntryLine_idAccountingEntryLine",
                        column: x => x.idAccountingEntryLine,
                        principalTable: "accountingEntryLine",
                        principalColumn: "idAccountingEntryLine",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineEntry_salesInvoiceLine_idSalesInvoiceLine",
                        column: x => x.idSalesInvoiceLine,
                        principalTable: "salesInvoiceLine",
                        principalColumn: "idSalesInvoiceLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla pivot N:M salesInvoiceLine ↔ accountingEntryLine.");

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[,]
                {
                    { 116, false, "4.5", 4, true, 2, "Ingresos por Ventas", "Ingreso" },
                    { 118, false, "5.15", 5, true, 2, "Costo de Ventas", "Gasto" },
                    { 117, true, "4.5.01", 116, true, 3, "Ingresos por Ventas — Mercadería", "Ingreso" },
                    { 119, true, "5.15.01", 118, true, 3, "Costo de Ventas — Mercadería", "Gasto" }
                });

            migrationBuilder.InsertData(
                table: "salesInvoiceType",
                columns: new[] { "idSalesInvoiceType", "codeSalesInvoiceType", "counterpartFromBankMovement", "idAccountCOGS", "idAccountCounterpartCRC", "idAccountCounterpartUSD", "idAccountInventory", "idAccountSalesRevenue", "idBankMovementType", "isActive", "nameSalesInvoiceType" },
                values: new object[,]
                {
                    { 1, "CONTADO_CRC", false, 119, 106, null, 109, 117, null, true, "Venta Contado CRC" },
                    { 2, "CONTADO_USD", false, 119, null, 107, 109, 117, null, true, "Venta Contado USD" },
                    { 3, "CREDITO", true, 119, null, null, 109, 117, null, true, "Venta a Crédito" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_bankMovementDocument_idSalesInvoice",
                table: "bankMovementDocument",
                column: "idSalesInvoice",
                filter: "[idSalesInvoice] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bankMovementDocument_typeDocument",
                table: "bankMovementDocument",
                sql: "typeDocument IN ('FacturaCompra', 'FacturaVenta', 'Recibo', 'Transferencia', 'Cheque', 'Otro')");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idBankAccount",
                table: "salesInvoice",
                column: "idBankAccount");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idContact",
                table: "salesInvoice",
                column: "idContact",
                filter: "[idContact] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idCurrency",
                table: "salesInvoice",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idFiscalPeriod",
                table: "salesInvoice",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idSalesInvoiceType",
                table: "salesInvoice",
                column: "idSalesInvoiceType");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoice_numberInvoice",
                table: "salesInvoice",
                column: "numberInvoice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceEntry_idAccountingEntry",
                table: "salesInvoiceEntry",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceEntry_idSalesInvoice_idAccountingEntry",
                table: "salesInvoiceEntry",
                columns: new[] { "idSalesInvoice", "idAccountingEntry" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idInventoryLot",
                table: "salesInvoiceLine",
                column: "idInventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idProduct",
                table: "salesInvoiceLine",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idSalesInvoice",
                table: "salesInvoiceLine",
                column: "idSalesInvoice");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idUnit",
                table: "salesInvoiceLine",
                column: "idUnit");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineEntry_idAccountingEntryLine",
                table: "salesInvoiceLineEntry",
                column: "idAccountingEntryLine");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceLineEntry_idSalesInvoiceLine_idAccountingEntryLine",
                table: "salesInvoiceLineEntry",
                columns: new[] { "idSalesInvoiceLine", "idAccountingEntryLine" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountCOGS",
                table: "salesInvoiceType",
                column: "idAccountCOGS");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountCounterpartCRC",
                table: "salesInvoiceType",
                column: "idAccountCounterpartCRC",
                filter: "[idAccountCounterpartCRC] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountCounterpartUSD",
                table: "salesInvoiceType",
                column: "idAccountCounterpartUSD",
                filter: "[idAccountCounterpartUSD] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountInventory",
                table: "salesInvoiceType",
                column: "idAccountInventory");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountSalesRevenue",
                table: "salesInvoiceType",
                column: "idAccountSalesRevenue");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idBankMovementType",
                table: "salesInvoiceType",
                column: "idBankMovementType",
                filter: "[idBankMovementType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceType_codeSalesInvoiceType",
                table: "salesInvoiceType",
                column: "codeSalesInvoiceType",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bankMovementDocument_salesInvoice_idSalesInvoice",
                table: "bankMovementDocument",
                column: "idSalesInvoice",
                principalTable: "salesInvoice",
                principalColumn: "idSalesInvoice",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bankMovementDocument_salesInvoice_idSalesInvoice",
                table: "bankMovementDocument");

            migrationBuilder.DropTable(
                name: "salesInvoiceEntry");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineEntry");

            migrationBuilder.DropTable(
                name: "salesInvoiceLine");

            migrationBuilder.DropTable(
                name: "salesInvoice");

            migrationBuilder.DropTable(
                name: "salesInvoiceType");

            migrationBuilder.DropIndex(
                name: "IX_bankMovementDocument_idSalesInvoice",
                table: "bankMovementDocument");

            migrationBuilder.DropCheckConstraint(
                name: "CK_bankMovementDocument_typeDocument",
                table: "bankMovementDocument");

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 118);

            migrationBuilder.DropColumn(
                name: "idSalesInvoice",
                table: "bankMovementDocument");

            migrationBuilder.AlterColumn<string>(
                name: "typeDocument",
                table: "bankMovementDocument",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                comment: "Tipo de documento: 'FacturaCompra', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldComment: "Tipo de documento: 'FacturaCompra', 'FacturaVenta', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bankMovementDocument_typeDocument",
                table: "bankMovementDocument",
                sql: "typeDocument IN ('FacturaCompra', 'Recibo', 'Transferencia', 'Cheque', 'Otro')");
        }
    }
}
