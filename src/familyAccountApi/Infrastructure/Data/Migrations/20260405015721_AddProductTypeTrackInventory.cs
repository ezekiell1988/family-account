using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypeTrackInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "productType",
                comment: "Tipo de producto según la fase productiva: Materia Prima, Producto en Proceso, Producto Terminado, Reventa o Servicios. Catálogo de sistema, sin CRUD expuesto al usuario.",
                oldComment: "Tipo de producto según la fase productiva: Materia Prima, Producto en Proceso, Producto Terminado o Reventa. Catálogo de sistema, sin CRUD expuesto al usuario.");

            migrationBuilder.AlterColumn<string>(
                name: "nameProductType",
                table: "productType",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                comment: "Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa | Servicios.",
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60,
                oldComment: "Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa.");

            migrationBuilder.AddColumn<bool>(
                name: "trackInventory",
                table: "productType",
                type: "bit",
                nullable: false,
                defaultValue: true,
                comment: "Indica si los productos de este tipo llevan control de stock (inventariables). false = Servicios y productos sin inventario.");

            migrationBuilder.UpdateData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 1,
                column: "trackInventory",
                value: true);

            migrationBuilder.UpdateData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 2,
                column: "trackInventory",
                value: true);

            migrationBuilder.UpdateData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 3,
                column: "trackInventory",
                value: true);

            migrationBuilder.UpdateData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 4,
                column: "trackInventory",
                value: true);

            migrationBuilder.InsertData(
                table: "productType",
                columns: new[] { "idProductType", "descriptionProductType", "nameProductType", "trackInventory" },
                values: new object[] { 5, "Servicios, mano de obra o conceptos sin stock físico. No generan movimientos de inventario.", "Servicios", false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "productType",
                keyColumn: "idProductType",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "trackInventory",
                table: "productType");

            migrationBuilder.AlterTable(
                name: "productType",
                comment: "Tipo de producto según la fase productiva: Materia Prima, Producto en Proceso, Producto Terminado o Reventa. Catálogo de sistema, sin CRUD expuesto al usuario.",
                oldComment: "Tipo de producto según la fase productiva: Materia Prima, Producto en Proceso, Producto Terminado, Reventa o Servicios. Catálogo de sistema, sin CRUD expuesto al usuario.");

            migrationBuilder.AlterColumn<string>(
                name: "nameProductType",
                table: "productType",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                comment: "Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa.",
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60,
                oldComment: "Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa | Servicios.");
        }
    }
}
