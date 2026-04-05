using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "rowVersion",
                table: "product",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                comment: "Token de concurrencia optimista. Previene race conditions al recalcular AverageCost en confirmaciones paralelas.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rowVersion",
                table: "product");
        }
    }
}
