using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLotStatusAndProductReorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "reorderPoint",
                table: "product",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true,
                comment: "Punto de reorden: stock mínimo que dispara una alerta de reabastecimiento. NULL si no aplica.");

            migrationBuilder.AddColumn<decimal>(
                name: "reorderQuantity",
                table: "product",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true,
                comment: "Cantidad sugerida a pedir cuando el stock cae por debajo del punto de reorden. NULL si no aplica.");

            migrationBuilder.AddColumn<decimal>(
                name: "safetyStock",
                table: "product",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true,
                comment: "Stock de seguridad reservado que no debe consumirse en operación normal. NULL si no aplica.");

            migrationBuilder.AddColumn<string>(
                name: "statusLot",
                table: "inventoryLot",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Disponible",
                comment: "Estado de calidad del lote: Disponible | Cuarentena | Bloqueado | Vencido. Solo los lotes Disponibles son seleccionables en FEFO.");

            migrationBuilder.AddCheckConstraint(
                name: "CK_inventoryLot_statusLot",
                table: "inventoryLot",
                sql: "statusLot IN ('Disponible', 'Cuarentena', 'Bloqueado', 'Vencido')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_inventoryLot_statusLot",
                table: "inventoryLot");

            migrationBuilder.DropColumn(
                name: "reorderPoint",
                table: "product");

            migrationBuilder.DropColumn(
                name: "reorderQuantity",
                table: "product");

            migrationBuilder.DropColumn(
                name: "safetyStock",
                table: "product");

            migrationBuilder.DropColumn(
                name: "statusLot",
                table: "inventoryLot");
        }
    }
}
