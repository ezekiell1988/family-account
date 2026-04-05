using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAdjustmentLineProductLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_inventoryAdjustmentLine_unitCostNew",
                table: "inventoryAdjustmentLine");

            migrationBuilder.AlterTable(
                name: "inventoryAdjustmentLine",
                comment: "Líneas del ajuste de inventario. Cada línea referencia un lote (idInventoryLot) o un producto (idProduct), nunca ambos. quantityDelta positivo = entrada, negativo = salida, cero = ajuste de costo puro. Ajuste por lote: unitCostNew requerido si quantityDelta > 0. Ajuste por producto (idProduct): quantityDelta siempre 0 y unitCostNew = costo promedio objetivo; ajusta todos los lotes del producto proporcionalmente.",
                oldComment: "Líneas del ajuste de inventario. Cada línea referencia un lote específico: quantityDelta positivo = entrada, negativo = salida, cero = ajuste de costo puro. Si quantityDelta > 0, unitCostNew es requerido.");

            migrationBuilder.AlterColumn<decimal>(
                name: "unitCostNew",
                table: "inventoryAdjustmentLine",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true,
                comment: "Costo unitario nuevo (ajuste por lote) o costo promedio objetivo (ajuste por producto). Requerido si quantityDelta > 0 o si se usa idProduct.",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldNullable: true,
                oldComment: "Nuevo costo unitario para el lote. Requerido si quantityDelta > 0. Si informado: reemplaza inventoryLot.unitCost.");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantityDelta",
                table: "inventoryAdjustmentLine",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                comment: "Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro. Siempre 0 para líneas por producto.",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldComment: "Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro (no mueve stock).");

            migrationBuilder.AlterColumn<int>(
                name: "idInventoryLot",
                table: "inventoryAdjustmentLine",
                type: "int",
                nullable: true,
                comment: "FK al lote de inventario a ajustar. Exclusivo con idProduct.",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "FK al lote de inventario a ajustar. Para líneas positivas que crean un lote nuevo, se crea el lote primero.");

            migrationBuilder.AddColumn<int>(
                name: "idProduct",
                table: "inventoryAdjustmentLine",
                type: "int",
                nullable: true,
                comment: "FK al producto para ajuste de costo promedio global. Exclusivo con idInventoryLot. Al confirmar: escala el unitCost de todos sus lotes proporcionalmente para que el costo promedio ponderado = unitCostNew.");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustmentLine_idProduct",
                table: "inventoryAdjustmentLine",
                column: "idProduct",
                filter: "[idProduct] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_inventoryAdjustmentLine_productLevel",
                table: "inventoryAdjustmentLine",
                sql: "idProduct IS NULL OR (quantityDelta = 0 AND unitCostNew IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_inventoryAdjustmentLine_target",
                table: "inventoryAdjustmentLine",
                sql: "(idInventoryLot IS NOT NULL AND idProduct IS NULL) OR (idInventoryLot IS NULL AND idProduct IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_inventoryAdjustmentLine_unitCostNew",
                table: "inventoryAdjustmentLine",
                sql: "idInventoryLot IS NULL OR quantityDelta <= 0 OR unitCostNew IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_inventoryAdjustmentLine_product_idProduct",
                table: "inventoryAdjustmentLine",
                column: "idProduct",
                principalTable: "product",
                principalColumn: "idProduct",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventoryAdjustmentLine_product_idProduct",
                table: "inventoryAdjustmentLine");

            migrationBuilder.DropIndex(
                name: "IX_inventoryAdjustmentLine_idProduct",
                table: "inventoryAdjustmentLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_inventoryAdjustmentLine_productLevel",
                table: "inventoryAdjustmentLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_inventoryAdjustmentLine_target",
                table: "inventoryAdjustmentLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_inventoryAdjustmentLine_unitCostNew",
                table: "inventoryAdjustmentLine");

            migrationBuilder.DropColumn(
                name: "idProduct",
                table: "inventoryAdjustmentLine");

            migrationBuilder.AlterTable(
                name: "inventoryAdjustmentLine",
                comment: "Líneas del ajuste de inventario. Cada línea referencia un lote específico: quantityDelta positivo = entrada, negativo = salida, cero = ajuste de costo puro. Si quantityDelta > 0, unitCostNew es requerido.",
                oldComment: "Líneas del ajuste de inventario. Cada línea referencia un lote (idInventoryLot) o un producto (idProduct), nunca ambos. quantityDelta positivo = entrada, negativo = salida, cero = ajuste de costo puro. Ajuste por lote: unitCostNew requerido si quantityDelta > 0. Ajuste por producto (idProduct): quantityDelta siempre 0 y unitCostNew = costo promedio objetivo; ajusta todos los lotes del producto proporcionalmente.");

            migrationBuilder.AlterColumn<decimal>(
                name: "unitCostNew",
                table: "inventoryAdjustmentLine",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true,
                comment: "Nuevo costo unitario para el lote. Requerido si quantityDelta > 0. Si informado: reemplaza inventoryLot.unitCost.",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldNullable: true,
                oldComment: "Costo unitario nuevo (ajuste por lote) o costo promedio objetivo (ajuste por producto). Requerido si quantityDelta > 0 o si se usa idProduct.");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantityDelta",
                table: "inventoryAdjustmentLine",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                comment: "Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro (no mueve stock).",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldComment: "Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro. Siempre 0 para líneas por producto.");

            migrationBuilder.AlterColumn<int>(
                name: "idInventoryLot",
                table: "inventoryAdjustmentLine",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "FK al lote de inventario a ajustar. Para líneas positivas que crean un lote nuevo, se crea el lote primero.",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "FK al lote de inventario a ajustar. Exclusivo con idProduct.");

            migrationBuilder.AddCheckConstraint(
                name: "CK_inventoryAdjustmentLine_unitCostNew",
                table: "inventoryAdjustmentLine",
                sql: "quantityDelta <= 0 OR unitCostNew IS NOT NULL");
        }
    }
}
