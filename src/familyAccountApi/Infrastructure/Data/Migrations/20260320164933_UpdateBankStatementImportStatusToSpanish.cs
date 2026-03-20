using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBankStatementImportStatusToSpanish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_bankStatementImport_status",
                table: "bankStatementImport");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "bankStatementImport",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Pendiente",
                comment: "Estado de la importación: Pendiente, Procesando, Completado, Fallido",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldDefaultValue: "Pending",
                oldComment: "Estado de la importación: Pending, Processing, Completed, Failed");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bankStatementImport_status",
                table: "bankStatementImport",
                sql: "status IN ('Pendiente', 'Procesando', 'Completado', 'Fallido')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_bankStatementImport_status",
                table: "bankStatementImport");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "bankStatementImport",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                comment: "Estado de la importación: Pending, Processing, Completed, Failed",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldDefaultValue: "Pendiente",
                oldComment: "Estado de la importación: Pendiente, Procesando, Completado, Fallido");

            migrationBuilder.AddCheckConstraint(
                name: "CK_bankStatementImport_status",
                table: "bankStatementImport",
                sql: "status IN ('Pending', 'Processing', 'Completed', 'Failed')");
        }
    }
}
