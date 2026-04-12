using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBacXlsTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "bankStatementTemplate",
                columns: new[] { "idBankStatementTemplate", "bankName", "codeTemplate", "columnMappings", "dateFormat", "isActive", "keywordRules", "nameTemplate", "notes", "timeFormat" },
                values: new object[] { 6, "BAC Credomatic", "BAC-XLS-V1", "{}", "dd/MM/yyyy", true, "[\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\n                                            \"idBankMovementType\":1,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"DEP_ATM\",\"TATMFULL\",\"DEPOSITO ATM\"],\n                                            \"idBankMovementType\":2,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TEF DE:\",\"DTR SINPE\",\"SINPE REC\",\"ABONO SINPE\",\"CREDITO SINPE\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"COOPEALIANZA\",\"CAJA AHORRO\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO \"],\n                                            \"idBankMovementType\":6,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"DTR:\",\"RETIRO CAJERO\",\"RETIRO ATM\",\"RETIRO EFECTIVO\"],\n                                            \"idBankMovementType\":8,\"matchMode\":\"Any\"}\n]", "BAC Credomatic – Cuenta de Ahorro/Débito (XLS)", "Archivo .xls (BIFF8) exportado desde el portal BAC para cuentas de ahorro y débito. Columnas fijas: Fecha | Referencia | | Código | Descripción | | | Débitos | Créditos | Balance. Usar para cuentas de ahorro BAC (cuenta CR73... en CRC).", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 6);
        }
    }
}
