using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRecipeVersionNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_productRecipe_idProductOutput",
                table: "productRecipe");

            migrationBuilder.AlterColumn<bool>(
                name: "isActive",
                table: "productRecipe",
                type: "bit",
                nullable: false,
                defaultValue: true,
                comment: "Solo recetas activas se usan en producción. Al actualizar una receta la versión anterior queda IsActive=false.",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true,
                oldComment: "Solo recetas activas se usan en producción.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "productRecipe",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                comment: "Fecha y hora UTC de creación de esta versión.",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()",
                oldComment: "Fecha y hora UTC de creación del registro.");

            migrationBuilder.AddColumn<int>(
                name: "versionNumber",
                table: "productRecipe",
                type: "int",
                nullable: false,
                defaultValue: 1,
                comment: "Número de versión de la receta. Se incrementa al actualizar. Cada modificación crea una nueva fila; la anterior queda IsActive=false.");

            migrationBuilder.CreateIndex(
                name: "UQ_productRecipe_idProductOutput_versionNumber",
                table: "productRecipe",
                columns: new[] { "idProductOutput", "versionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_productRecipe_idProductOutput_versionNumber",
                table: "productRecipe");

            migrationBuilder.DropColumn(
                name: "versionNumber",
                table: "productRecipe");

            migrationBuilder.AlterColumn<bool>(
                name: "isActive",
                table: "productRecipe",
                type: "bit",
                nullable: false,
                defaultValue: true,
                comment: "Solo recetas activas se usan en producción.",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true,
                oldComment: "Solo recetas activas se usan en producción. Al actualizar una receta la versión anterior queda IsActive=false.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "createdAt",
                table: "productRecipe",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                comment: "Fecha y hora UTC de creación del registro.",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()",
                oldComment: "Fecha y hora UTC de creación de esta versión.");

            migrationBuilder.CreateIndex(
                name: "IX_productRecipe_idProductOutput",
                table: "productRecipe",
                column: "idProductOutput");
        }
    }
}
