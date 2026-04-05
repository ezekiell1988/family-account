using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddC6SlotOptionIsPreset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isPreset",
                table: "salesOrderLineSlotOption",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "true = opción copiada automáticamente del preset del slot; false = elegida libremente por el cliente.");

            migrationBuilder.AddColumn<bool>(
                name: "isPreset",
                table: "salesInvoiceLineSlotOption",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "true = opción copiada automáticamente del preset del slot; false = elegida libremente por el cliente.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isPreset",
                table: "salesOrderLineSlotOption");

            migrationBuilder.DropColumn(
                name: "isPreset",
                table: "salesInvoiceLineSlotOption");
        }
    }
}
