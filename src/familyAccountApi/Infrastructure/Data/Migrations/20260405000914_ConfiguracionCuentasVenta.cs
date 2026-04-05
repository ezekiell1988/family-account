using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracionCuentasVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "codeSalesInvoiceType",
                table: "salesInvoiceType",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                comment: "Código único del tipo: 'CONTADO_CRC', 'CONTADO_USD', 'CREDITO_CRC', 'CREDITO_USD'.",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldComment: "Código único del tipo: 'CONTADO_CRC', 'CONTADO_USD', 'CREDITO'.");

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[] { 120, false, "1.1.08", 7, true, 3, "Cuentas por Cobrar", "Activo" });

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[,]
                {
                    { 121, true, "1.1.08.01", 120, true, 4, "Cuentas por Cobrar — Clientes CRC (₡)", "Activo" },
                    { 122, true, "1.1.08.02", 120, true, 4, "Cuentas por Cobrar — Clientes USD ($)", "Activo" }
                });

            migrationBuilder.InsertData(
                table: "bankMovementType",
                columns: new[] { "idBankMovementType", "codeBankMovementType", "idAccountCounterpart", "isActive", "movementSign", "nameBankMovementType" },
                values: new object[,]
                {
                    { 9, "COBRO-CRC", 121, true, "Abono", "Cobro de Venta a Crédito (₡)" },
                    { 10, "COBRO-USD", 122, true, "Abono", "Cobro de Venta a Crédito ($)" }
                });

            migrationBuilder.UpdateData(
                table: "salesInvoiceType",
                keyColumn: "idSalesInvoiceType",
                keyValue: 3,
                columns: new[] { "codeSalesInvoiceType", "idBankMovementType", "nameSalesInvoiceType" },
                values: new object[] { "CREDITO_CRC", 9, "Venta a Crédito CRC (₡)" });

            migrationBuilder.InsertData(
                table: "salesInvoiceType",
                columns: new[] { "idSalesInvoiceType", "codeSalesInvoiceType", "counterpartFromBankMovement", "idAccountCOGS", "idAccountCounterpartCRC", "idAccountCounterpartUSD", "idAccountInventory", "idAccountSalesRevenue", "idBankMovementType", "isActive", "nameSalesInvoiceType" },
                values: new object[] { 4, "CREDITO_USD", true, 119, null, null, 109, 117, 10, true, "Venta a Crédito USD ($)" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "bankMovementType",
                keyColumn: "idBankMovementType",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "salesInvoiceType",
                keyColumn: "idSalesInvoiceType",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "bankMovementType",
                keyColumn: "idBankMovementType",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "idAccount",
                keyValue: 120);

            migrationBuilder.AlterColumn<string>(
                name: "codeSalesInvoiceType",
                table: "salesInvoiceType",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                comment: "Código único del tipo: 'CONTADO_CRC', 'CONTADO_USD', 'CREDITO'.",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldComment: "Código único del tipo: 'CONTADO_CRC', 'CONTADO_USD', 'CREDITO_CRC', 'CREDITO_USD'.");

            migrationBuilder.UpdateData(
                table: "salesInvoiceType",
                keyColumn: "idSalesInvoiceType",
                keyValue: 3,
                columns: new[] { "codeSalesInvoiceType", "idBankMovementType", "nameSalesInvoiceType" },
                values: new object[] { "CREDITO", null, "Venta a Crédito" });
        }
    }
}
