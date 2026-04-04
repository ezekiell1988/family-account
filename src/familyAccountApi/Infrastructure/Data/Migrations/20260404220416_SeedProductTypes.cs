using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "productType",
                columns: new[] { "idProductType", "descriptionProductType", "nameProductType" },
                values: new object[,]
                {
                    { 1, "Insumos o materiales adquiridos para ser utilizados en el proceso productivo. No se venden directamente.", "Materia Prima" },
                    { 2, "Productos que han iniciado su proceso de fabricación pero aún no están terminados.", "Producto en Proceso" },
                    { 3, "Productos que han completado el proceso productivo y están listos para la venta.", "Producto Terminado" },
                    { 4, "Productos adquiridos listos para la venta sin transformación productiva.", "Reventa" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 4);
        }
    }
}
