using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrdersProductionOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idSalesOrder",
                table: "salesInvoice",
                type: "int",
                nullable: true,
                comment: "FK al pedido de venta que origina esta factura. NULL = venta directa de tienda.");

            migrationBuilder.AddColumn<int>(
                name: "idProductionOrder",
                table: "inventoryAdjustment",
                type: "int",
                nullable: true,
                comment: "FK a la orden de produccion que originó este ajuste. NULL = modalidad A (producción para stock).");

            migrationBuilder.CreateTable(
                name: "priceList",
                columns: table => new
                {
                    idPriceList = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la lista de precios.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    namePriceList = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre descriptivo de la lista (ej: Lista Mayorista Abril 2026)."),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Descripción opcional de la lista."),
                    dateFrom = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de inicio de vigencia."),
                    dateTo = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha de fin de vigencia. NULL = vigente indefinidamente."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si la lista está activa para su uso."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_priceList", x => x.idPriceList);
                },
                comment: "Lista de precios con vigencia por fechas. Al crear un pedido se hace snapshot del precio vigente en SalesOrderLine.UnitPrice.");

            migrationBuilder.CreateTable(
                name: "priceListItem",
                columns: table => new
                {
                    idPriceListItem = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del ítem.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idPriceList = table.Column<int>(type: "int", nullable: false, comment: "FK a la lista de precios a la que pertenece este ítem."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto."),
                    idProductUnit = table.Column<int>(type: "int", nullable: false, comment: "FK a la presentación (unidad de venta) del producto."),
                    unitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Precio unitario del producto en esta presentación y lista."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si el ítem está activo."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_priceListItem", x => x.idPriceListItem);
                    table.ForeignKey(
                        name: "FK_priceListItem_priceList_idPriceList",
                        column: x => x.idPriceList,
                        principalTable: "priceList",
                        principalColumn: "idPriceList",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_priceListItem_productUnit_idProductUnit",
                        column: x => x.idProductUnit,
                        principalTable: "productUnit",
                        principalColumn: "idProductUnit",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_priceListItem_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Ítem de lista de precios: precio unitario por producto y presentación (ProductUnit) dentro de una lista.");

            migrationBuilder.CreateTable(
                name: "salesOrder",
                columns: table => new
                {
                    idSalesOrder = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del pedido.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda del pedido."),
                    idContact = table.Column<int>(type: "int", nullable: false, comment: "FK al cliente que realiza el pedido."),
                    idPriceList = table.Column<int>(type: "int", nullable: true, comment: "FK a la lista de precios vigente al crear el pedido. Sirve como referencia; el precio real queda en SalesOrderLine.UnitPrice."),
                    numberOrder = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número correlativo del pedido (ej: PED-2026-0001)."),
                    dateOrder = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha en que se ingresó el pedido."),
                    dateDelivery = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha compromiso de entrega al cliente. Nullable."),
                    subTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Subtotal sin impuesto."),
                    taxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto total de impuesto."),
                    totalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total del pedido (subtotal + impuesto)."),
                    exchangeRateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Tipo de cambio al momento de crear el pedido."),
                    statusOrder = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Borrador", comment: "Estado del pedido: Borrador | Confirmado | EnProduccion | Completado | Anulado."),
                    descriptionOrder = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Observaciones opcionales del pedido."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrder", x => x.idSalesOrder);
                    table.CheckConstraint("CK_salesOrder_statusOrder", "statusOrder IN ('Borrador', 'Confirmado', 'EnProduccion', 'Completado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_salesOrder_contact_idContact",
                        column: x => x.idContact,
                        principalTable: "contact",
                        principalColumn: "idContact",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrder_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrder_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrder_priceList_idPriceList",
                        column: x => x.idPriceList,
                        principalTable: "priceList",
                        principalColumn: "idPriceList",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Pedido de un cliente externo. Modalidad B de producción: permite mezclar stock existente y órdenes de producción para cumplir el pedido. Flujo: Borrador → Confirmado → EnProduccion → Completado → Anulado.");

            migrationBuilder.CreateTable(
                name: "productionOrder",
                columns: table => new
                {
                    idProductionOrder = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la orden de producción.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que corresponde la orden."),
                    idSalesOrder = table.Column<int>(type: "int", nullable: true, comment: "FK al pedido de venta que origina esta orden. NULL = producción para stock (Modalidad A)."),
                    numberProductionOrder = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número correlativo de la orden (ej: OP-2026-0001)."),
                    dateOrder = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de creación de la orden de producción."),
                    dateRequired = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha en que se necesita tener disponible lo producido. Nullable."),
                    statusProductionOrder = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Borrador", comment: "Estado: Borrador | Pendiente | EnProceso | Completado | Anulado."),
                    descriptionOrder = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Observaciones opcionales."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productionOrder", x => x.idProductionOrder);
                    table.CheckConstraint("CK_productionOrder_statusProductionOrder", "statusProductionOrder IN ('Borrador', 'Pendiente', 'EnProceso', 'Completado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_productionOrder_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionOrder_salesOrder_idSalesOrder",
                        column: x => x.idSalesOrder,
                        principalTable: "salesOrder",
                        principalColumn: "idSalesOrder",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Orden de producción. IdSalesOrder = NULL → Modalidad A (producción para stock); IdSalesOrder NOT NULL → Modalidad B (contra pedido). Permite múltiples corridas de InventoryAdjustment tipo PRODUCCION bajo la misma orden.");

            migrationBuilder.CreateTable(
                name: "salesOrderLine",
                columns: table => new
                {
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrder = table.Column<int>(type: "int", nullable: false, comment: "FK al pedido de venta cabecera."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto solicitado."),
                    idProductUnit = table.Column<int>(type: "int", nullable: false, comment: "FK a la presentación (unidad de venta) en que se pide el producto."),
                    idPriceListItem = table.Column<int>(type: "int", nullable: true, comment: "FK al ítem de lista de precios del que se tomó el precio. NULL si se ingresó manualmente."),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad en la unidad de venta solicitada."),
                    quantityBase = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad en unidad base, calculada × ConversionFactor al confirmar."),
                    unitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Snapshot del precio unitario al crear el pedido. No cambia aunque la lista de precios se actualice."),
                    taxPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "Porcentaje de impuesto al momento del pedido (ej: 13.00)."),
                    totalLineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total de la línea (Quantity × UnitPrice × (1 + TaxPercent / 100))."),
                    descriptionLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Descripción o nota adicional de la línea.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLine", x => x.idSalesOrderLine);
                    table.ForeignKey(
                        name: "FK_salesOrderLine_priceListItem_idPriceListItem",
                        column: x => x.idPriceListItem,
                        principalTable: "priceListItem",
                        principalColumn: "idPriceListItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLine_productUnit_idProductUnit",
                        column: x => x.idProductUnit,
                        principalTable: "productUnit",
                        principalColumn: "idProductUnit",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLine_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLine_salesOrder_idSalesOrder",
                        column: x => x.idSalesOrder,
                        principalTable: "salesOrder",
                        principalColumn: "idSalesOrder",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Línea del pedido de venta. UnitPrice es snapshot del precio de la lista vigente al crear el pedido.");

            migrationBuilder.CreateTable(
                name: "salesOrderAdvance",
                columns: table => new
                {
                    idSalesOrderAdvance = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del anticipo.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrder = table.Column<int>(type: "int", nullable: false, comment: "FK al pedido de venta al que corresponde este anticipo."),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false, comment: "FK al asiento contable que registra la recepción del anticipo."),
                    idProductionOrder = table.Column<int>(type: "int", nullable: true, comment: "FK informativa a la orden de producción en cuyo contexto se recibió el anticipo. No afecta la lógica financiera."),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto del anticipo en la moneda del pedido."),
                    dateAdvance = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha en que se recibió el anticipo."),
                    descriptionAdvance = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Nota opcional sobre el anticipo."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderAdvance", x => x.idSalesOrderAdvance);
                    table.ForeignKey(
                        name: "FK_salesOrderAdvance_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderAdvance_productionOrder_idProductionOrder",
                        column: x => x.idProductionOrder,
                        principalTable: "productionOrder",
                        principalColumn: "idProductionOrder",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderAdvance_salesOrder_idSalesOrder",
                        column: x => x.idSalesOrder,
                        principalTable: "salesOrder",
                        principalColumn: "idSalesOrder",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Anticipo o depósito recibido de un cliente contra un pedido de venta. Se aplica como crédito al emitir la SalesInvoice final. IdProductionOrder es contexto informativo sobre cuándo/por qué se recibió el anticipo.");

            migrationBuilder.CreateTable(
                name: "productionOrderLine",
                columns: table => new
                {
                    idProductionOrderLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductionOrder = table.Column<int>(type: "int", nullable: false, comment: "FK a la orden de producción cabecera."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto final a producir en esta línea."),
                    idProductUnit = table.Column<int>(type: "int", nullable: false, comment: "FK a la unidad de producción del producto."),
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: true, comment: "FK opcional a la línea del pedido de venta que originó esta línea de producción."),
                    quantityRequired = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad total comprometida en unidad base que se debe producir para cubrir el pedido."),
                    quantityProduced = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m, comment: "Acumulado producido. Se incrementa cada vez que se confirma un InventoryAdjustment vinculado a esta orden."),
                    descriptionLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Nota adicional opcional de la línea.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productionOrderLine", x => x.idProductionOrderLine);
                    table.ForeignKey(
                        name: "FK_productionOrderLine_productUnit_idProductUnit",
                        column: x => x.idProductUnit,
                        principalTable: "productUnit",
                        principalColumn: "idProductUnit",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionOrderLine_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionOrderLine_productionOrder_idProductionOrder",
                        column: x => x.idProductionOrder,
                        principalTable: "productionOrder",
                        principalColumn: "idProductionOrder",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productionOrderLine_salesOrderLine_idSalesOrderLine",
                        column: x => x.idSalesOrderLine,
                        principalTable: "salesOrderLine",
                        principalColumn: "idSalesOrderLine",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Línea de orden de producción. Registra qué producto producir, cuánto se requiere (QuantityRequired) y cuánto se ha producido acumulado (QuantityProduced). Vinculada opcionalmente a la línea del pedido de origen para calcular margen por pedido.");

            migrationBuilder.CreateTable(
                name: "salesOrderLineFulfillment",
                columns: table => new
                {
                    idSalesOrderLineFulfillment = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea del pedido que se está cumpliendo."),
                    fulfillmentType = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, comment: "Tipo: 'Stock' = se toma de un lote existente; 'Produccion' = se producirá contra esta línea."),
                    idInventoryLot = table.Column<int>(type: "int", nullable: true, comment: "FK al lote de inventario asignado cuando FulfillmentType = 'Stock'."),
                    idProductionOrder = table.Column<int>(type: "int", nullable: true, comment: "FK a la orden de producción cuando FulfillmentType = 'Produccion'."),
                    quantityBase = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad en unidad base asignada a este fulfillment."),
                    unitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "Snapshot del costo unitario al confirmar la factura final."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLineFulfillment", x => x.idSalesOrderLineFulfillment);
                    table.CheckConstraint("CK_salesOrderLineFulfillment_lot_or_order", "(fulfillmentType = 'Stock' AND idInventoryLot IS NOT NULL AND idProductionOrder IS NULL) OR (fulfillmentType = 'Produccion' AND idProductionOrder IS NOT NULL AND idInventoryLot IS NULL)");
                    table.CheckConstraint("CK_salesOrderLineFulfillment_type", "fulfillmentType IN ('Stock', 'Produccion')");
                    table.ForeignKey(
                        name: "FK_salesOrderLineFulfillment_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineFulfillment_productionOrder_idProductionOrder",
                        column: x => x.idProductionOrder,
                        principalTable: "productionOrder",
                        principalColumn: "idProductionOrder",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineFulfillment_salesOrderLine_idSalesOrderLine",
                        column: x => x.idSalesOrderLine,
                        principalTable: "salesOrderLine",
                        principalColumn: "idSalesOrderLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Detalle de cómo se cumple cada línea del pedido: con stock existente (FulfillmentType='Stock' → IdInventoryLot) o con producción planificada (FulfillmentType='Produccion' → IdProductionOrder). Una línea puede tener múltiples registros para mezclar stock y producción.");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idSalesOrder",
                table: "salesInvoice",
                column: "idSalesOrder",
                filter: "[idSalesOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustment_idProductionOrder",
                table: "inventoryAdjustment",
                column: "idProductionOrder",
                filter: "[idProductionOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_priceListItem_idProduct",
                table: "priceListItem",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_priceListItem_idProductUnit",
                table: "priceListItem",
                column: "idProductUnit");

            migrationBuilder.CreateIndex(
                name: "UQ_priceListItem_idPriceList_idProduct_idProductUnit",
                table: "priceListItem",
                columns: new[] { "idPriceList", "idProduct", "idProductUnit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productionOrder_idFiscalPeriod",
                table: "productionOrder",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrder_idSalesOrder",
                table: "productionOrder",
                column: "idSalesOrder",
                filter: "[idSalesOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_productionOrder_numberProductionOrder",
                table: "productionOrder",
                column: "numberProductionOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productionOrderLine_idProduct",
                table: "productionOrderLine",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrderLine_idProductionOrder",
                table: "productionOrderLine",
                column: "idProductionOrder");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrderLine_idProductUnit",
                table: "productionOrderLine",
                column: "idProductUnit");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrderLine_idSalesOrderLine",
                table: "productionOrderLine",
                column: "idSalesOrderLine",
                filter: "[idSalesOrderLine] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrder_idContact",
                table: "salesOrder",
                column: "idContact");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrder_idCurrency",
                table: "salesOrder",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrder_idFiscalPeriod",
                table: "salesOrder",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrder_idPriceList",
                table: "salesOrder",
                column: "idPriceList",
                filter: "[idPriceList] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_salesOrder_numberOrder",
                table: "salesOrder",
                column: "numberOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderAdvance_idAccountingEntry",
                table: "salesOrderAdvance",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderAdvance_idProductionOrder",
                table: "salesOrderAdvance",
                column: "idProductionOrder",
                filter: "[idProductionOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderAdvance_idSalesOrder",
                table: "salesOrderAdvance",
                column: "idSalesOrder");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLine_idPriceListItem",
                table: "salesOrderLine",
                column: "idPriceListItem",
                filter: "[idPriceListItem] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLine_idProduct",
                table: "salesOrderLine",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLine_idProductUnit",
                table: "salesOrderLine",
                column: "idProductUnit");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLine_idSalesOrder",
                table: "salesOrderLine",
                column: "idSalesOrder");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineFulfillment_idInventoryLot",
                table: "salesOrderLineFulfillment",
                column: "idInventoryLot",
                filter: "[idInventoryLot] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineFulfillment_idProductionOrder",
                table: "salesOrderLineFulfillment",
                column: "idProductionOrder",
                filter: "[idProductionOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineFulfillment_idSalesOrderLine",
                table: "salesOrderLineFulfillment",
                column: "idSalesOrderLine");

            migrationBuilder.AddForeignKey(
                name: "FK_inventoryAdjustment_productionOrder_idProductionOrder",
                table: "inventoryAdjustment",
                column: "idProductionOrder",
                principalTable: "productionOrder",
                principalColumn: "idProductionOrder",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_salesInvoice_salesOrder_idSalesOrder",
                table: "salesInvoice",
                column: "idSalesOrder",
                principalTable: "salesOrder",
                principalColumn: "idSalesOrder",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventoryAdjustment_productionOrder_idProductionOrder",
                table: "inventoryAdjustment");

            migrationBuilder.DropForeignKey(
                name: "FK_salesInvoice_salesOrder_idSalesOrder",
                table: "salesInvoice");

            migrationBuilder.DropTable(
                name: "productionOrderLine");

            migrationBuilder.DropTable(
                name: "salesOrderAdvance");

            migrationBuilder.DropTable(
                name: "salesOrderLineFulfillment");

            migrationBuilder.DropTable(
                name: "productionOrder");

            migrationBuilder.DropTable(
                name: "salesOrderLine");

            migrationBuilder.DropTable(
                name: "priceListItem");

            migrationBuilder.DropTable(
                name: "salesOrder");

            migrationBuilder.DropTable(
                name: "priceList");

            migrationBuilder.DropIndex(
                name: "IX_salesInvoice_idSalesOrder",
                table: "salesInvoice");

            migrationBuilder.DropIndex(
                name: "IX_inventoryAdjustment_idProductionOrder",
                table: "inventoryAdjustment");

            migrationBuilder.DropColumn(
                name: "idSalesOrder",
                table: "salesInvoice");

            migrationBuilder.DropColumn(
                name: "idProductionOrder",
                table: "inventoryAdjustment");
        }
    }
}
