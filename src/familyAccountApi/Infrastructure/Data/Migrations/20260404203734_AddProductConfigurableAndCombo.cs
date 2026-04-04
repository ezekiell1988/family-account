using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductConfigurableAndCombo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "salePrice",
                table: "productUnit",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                comment: "Precio base de venta para esta presentación. El precio final en combos/opciones se calcula sumando deltas.");

            migrationBuilder.AddColumn<bool>(
                name: "hasOptions",
                table: "product",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "Indica que el producto tiene grupos de opciones configurables por el cliente (ej: tamaño, masa, sabor).");

            migrationBuilder.AddColumn<bool>(
                name: "isCombo",
                table: "product",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "Indica que el producto es un combo compuesto de slots con productos elegibles.");

            migrationBuilder.CreateTable(
                name: "productComboSlot",
                columns: table => new
                {
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del slot del combo.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductCombo = table.Column<int>(type: "int", nullable: false, comment: "FK al producto combo padre (IsCombo=true)."),
                    nameSlot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre visible del slot (ej: Pizza #1, Bebida)."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este slot dentro del combo."),
                    isRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Si el cliente debe llenar este slot obligatoriamente."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación del slot al cliente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productComboSlot", x => x.idProductComboSlot);
                    table.ForeignKey(
                        name: "FK_productComboSlot_product_idProductCombo",
                        column: x => x.idProductCombo,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Slots de un combo (ej: Pizza #1, Pizza #2, Bebida). Un producto con IsCombo=true tiene N slots.");

            migrationBuilder.CreateTable(
                name: "productOptionGroup",
                columns: table => new
                {
                    idProductOptionGroup = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del grupo de opciones.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto configurable al que pertenece este grupo."),
                    nameGroup = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre visible del grupo (ej: Elige tu tamaño)."),
                    isRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Si el cliente debe elegir obligatoriamente en este grupo."),
                    minSelections = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "Mínimo de items a elegir. 0 para grupos opcionales."),
                    maxSelections = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "Máximo de items a elegir. 1 para exclusivo, N para múltiple."),
                    allowSplit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Cuando true, en modo mitad/mitad el cliente asigna cada selección a una mitad (half1|half2|whole). Aplica a grupos de sabor y adicionales."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación del grupo al cliente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productOptionGroup", x => x.idProductOptionGroup);
                    table.ForeignKey(
                        name: "FK_productOptionGroup_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Grupos de opciones configurables de un producto (ej: Tamaño, Masa, Sabor). Un producto con HasOptions=true puede tener N grupos.");

            migrationBuilder.CreateTable(
                name: "productComboSlotProduct",
                columns: table => new
                {
                    idProductComboSlotProduct = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "FK al slot del combo al que pertenece esta opción."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto permitido en este slot."),
                    priceAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m, comment: "Ajuste adicional al precio del combo por elegir este producto en el slot."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación dentro del slot.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productComboSlotProduct", x => x.idProductComboSlotProduct);
                    table.ForeignKey(
                        name: "FK_productComboSlotProduct_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productComboSlotProduct_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Productos permitidos en cada slot de un combo. El cliente elige uno de esta lista al armar el pedido.");

            migrationBuilder.CreateTable(
                name: "productOptionItem",
                columns: table => new
                {
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del item de opción.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductOptionGroup = table.Column<int>(type: "int", nullable: false, comment: "FK al grupo de opciones al que pertenece este item."),
                    nameItem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre visible de la opción (ej: Masa Delgada)."),
                    priceDelta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m, comment: "Ajuste de precio sobre el precio base del producto. Puede ser positivo, negativo o cero."),
                    isDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Opción marcada por defecto al abrir el selector."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación dentro del grupo.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productOptionItem", x => x.idProductOptionItem);
                    table.ForeignKey(
                        name: "FK_productOptionItem_productOptionGroup_idProductOptionGroup",
                        column: x => x.idProductOptionGroup,
                        principalTable: "productOptionGroup",
                        principalColumn: "idProductOptionGroup",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Cada opción dentro de un grupo configurable (ej: Delgada, Gruesa, Rellena dentro del grupo Masa).");

            migrationBuilder.CreateIndex(
                name: "IX_productComboSlot_idProductCombo",
                table: "productComboSlot",
                column: "idProductCombo");

            migrationBuilder.CreateIndex(
                name: "IX_productComboSlotProduct_idProduct",
                table: "productComboSlotProduct",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "UQ_productComboSlotProduct_idSlot_idProduct",
                table: "productComboSlotProduct",
                columns: new[] { "idProductComboSlot", "idProduct" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productOptionGroup_idProduct",
                table: "productOptionGroup",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_productOptionItem_idProductOptionGroup",
                table: "productOptionItem",
                column: "idProductOptionGroup");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "productComboSlotProduct");

            migrationBuilder.DropTable(
                name: "productOptionItem");

            migrationBuilder.DropTable(
                name: "productComboSlot");

            migrationBuilder.DropTable(
                name: "productOptionGroup");

            migrationBuilder.DropColumn(
                name: "salePrice",
                table: "productUnit");

            migrationBuilder.DropColumn(
                name: "hasOptions",
                table: "product");

            migrationBuilder.DropColumn(
                name: "isCombo",
                table: "product");
        }
    }
}
