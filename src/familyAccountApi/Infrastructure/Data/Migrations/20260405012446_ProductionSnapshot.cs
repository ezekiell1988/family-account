using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductionSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "productionSnapshot",
                columns: table => new
                {
                    idProductionSnapshot = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del snapshot de producción.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: false, comment: "FK 1:1 al ajuste de inventario de tipo PRODUCCION."),
                    idProductRecipe = table.Column<int>(type: "int", nullable: false, comment: "FK a la receta vigente al momento de confirmar la producción."),
                    quantityCalculated = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad teórica del producto final según la receta (ProductRecipe.QuantityOutput al confirmar)."),
                    quantityReal = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad real producida físicamente en esta corrida."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha y hora UTC en que se creó el snapshot.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productionSnapshot", x => x.idProductionSnapshot);
                    table.ForeignKey(
                        name: "FK_productionSnapshot_inventoryAdjustment_idInventoryAdjustment",
                        column: x => x.idInventoryAdjustment,
                        principalTable: "inventoryAdjustment",
                        principalColumn: "idInventoryAdjustment",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionSnapshot_productRecipe_idProductRecipe",
                        column: x => x.idProductRecipe,
                        principalTable: "productRecipe",
                        principalColumn: "idProductRecipe",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Copia de la receta usada al confirmar un ajuste de producción. Registra la cantidad calculada (teórica) y la real producida para permitir ajustar recetas a lo largo del tiempo.");

            migrationBuilder.CreateTable(
                name: "productionSnapshotLine",
                columns: table => new
                {
                    idProductionSnapshotLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductionSnapshot = table.Column<int>(type: "int", nullable: false, comment: "FK al snapshot de producción cabecera."),
                    idProductRecipeLine = table.Column<int>(type: "int", nullable: true, comment: "FK a la línea de receta de origen. NULL si es un insumo extra no previsto en la receta."),
                    idProductInput = table.Column<int>(type: "int", nullable: false, comment: "Snapshot del producto insumo, desacoplado de la línea de receta para sobrevivir cambios futuros en la misma."),
                    quantityCalculated = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad teórica: ProductRecipeLine.QuantityInput × (QuantityReal / QuantityCalculated de la cabecera). 0 para insumos extra."),
                    quantityReal = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad real usada por el operador en esta corrida."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de visualización, copiado de ProductRecipeLine.SortOrder.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productionSnapshotLine", x => x.idProductionSnapshotLine);
                    table.ForeignKey(
                        name: "FK_productionSnapshotLine_productRecipeLine_idProductRecipeLine",
                        column: x => x.idProductRecipeLine,
                        principalTable: "productRecipeLine",
                        principalColumn: "idProductRecipeLine",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_productionSnapshotLine_product_idProductInput",
                        column: x => x.idProductInput,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionSnapshotLine_productionSnapshot_idProductionSnapshot",
                        column: x => x.idProductionSnapshot,
                        principalTable: "productionSnapshot",
                        principalColumn: "idProductionSnapshot",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Línea del snapshot de producción. Una fila por insumo, con cantidad teórica calculada (según receta) y cantidad real usada. idProductRecipeLine es NULL cuando el operador agregó un insumo extra no previsto en la receta.");

            migrationBuilder.CreateIndex(
                name: "IX_productionSnapshot_idProductRecipe",
                table: "productionSnapshot",
                column: "idProductRecipe");

            migrationBuilder.CreateIndex(
                name: "UQ_productionSnapshot_idInventoryAdjustment",
                table: "productionSnapshot",
                column: "idInventoryAdjustment",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productionSnapshotLine_idProductInput",
                table: "productionSnapshotLine",
                column: "idProductInput");

            migrationBuilder.CreateIndex(
                name: "IX_productionSnapshotLine_idProductionSnapshot",
                table: "productionSnapshotLine",
                column: "idProductionSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_productionSnapshotLine_idProductRecipeLine",
                table: "productionSnapshotLine",
                column: "idProductRecipeLine",
                filter: "[idProductRecipeLine] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "productionSnapshotLine");

            migrationBuilder.DropTable(
                name: "productionSnapshot");
        }
    }
}
