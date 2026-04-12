using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBacXlsKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 6,
                column: "keywordRules",
                value: "[\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\n                                            \"idBankMovementType\":1,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"DEP_ATM\",\"TATMFULL\",\"DEPOSITO ATM\"],\n                                            \"idBankMovementType\":2,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TEF DE:\",\"DTR SINPE\",\"SINPE REC\",\"ABONO SINPE\",\"CREDITO SINPE\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"COOPEALIANZA\",\"CAJA AHORRO\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO \",\"SINPE MOVIL PAGO_TARJETA\"],\n                                            \"idBankMovementType\":6,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"DTR:\",\"RETIRO CAJERO\",\"RETIRO ATM\",\"RETIRO EFECTIVO\"],\n                                            \"idBankMovementType\":8,\"matchMode\":\"Any\"}\n]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 6,
                column: "keywordRules",
                value: "[\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\n                                            \"idBankMovementType\":1,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"DEP_ATM\",\"TATMFULL\",\"DEPOSITO ATM\"],\n                                            \"idBankMovementType\":2,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TEF DE:\",\"DTR SINPE\",\"SINPE REC\",\"ABONO SINPE\",\"CREDITO SINPE\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"COOPEALIANZA\",\"CAJA AHORRO\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO \"],\n                                            \"idBankMovementType\":6,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"DTR:\",\"RETIRO CAJERO\",\"RETIRO ATM\",\"RETIRO EFECTIVO\"],\n                                            \"idBankMovementType\":8,\"matchMode\":\"Any\"}\n]");
        }
    }
}
