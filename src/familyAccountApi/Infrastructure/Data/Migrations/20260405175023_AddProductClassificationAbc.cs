using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductClassificationAbc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "classificationAbc",
                table: "product",
                type: "CHAR(1)",
                unicode: false,
                maxLength: 1,
                nullable: true,
                comment: "Clasificación ABC calculada por Hangfire según valor de ventas de los últimos 90 días. A=top 80%, B=siguiente 15%, C=último 5%. NULL si sin ventas en el período.");

            migrationBuilder.AddCheckConstraint(
                name: "CK_product_classificationAbc",
                table: "product",
                sql: "[classificationAbc] IS NULL OR [classificationAbc] IN ('A', 'B', 'C')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_product_classificationAbc",
                table: "product");

            migrationBuilder.DropColumn(
                name: "classificationAbc",
                table: "product");
        }
    }
}
