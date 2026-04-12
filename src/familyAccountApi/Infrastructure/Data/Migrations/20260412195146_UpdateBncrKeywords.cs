using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBncrKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 3,
                column: "keywordRules",
                value: "[\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\n                                            \"idBankMovementType\":1,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"INTERESES GANADOS\"],\n                                            \"idBankMovementType\":2,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRANSFERENCIA SINPE\",\"SINPE MOVIL\",\"PAGO TARJETA BAC\",\"PAGOTARJETABAC\",\"SEMANA MAXIPAL\",\"PAGO SERVICIO PROFESIONAL\",\"PAGOSERVICIO\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"RETIRO ATM\",\"RETIRO CAJERO\",\"RETIRO EFECTIVO\"],\n                                            \"idBankMovementType\":5,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO TARJET\",\"PAGO TC\",\"TARJETA CRED\"],\n                                            \"idBankMovementType\":6,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO PREST\",\"CUOTA PREST\",\"PAGO PRESTAMO\",\"CUOTA PRESTAMO\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"SINPE MOVIL DEB\",\"DEB SINPE\",\"CARGO SINPE\",\"TRANSF DEB\"],\n                                            \"idBankMovementType\":8,\"matchMode\":\"Any\"}\n]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 3,
                column: "keywordRules",
                value: "[\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\n                                            \"idBankMovementType\":1,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"INTERESES GANADOS\"],\n                                            \"idBankMovementType\":2,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRANSFERENCIA SINPE\",\"SINPE MOVIL\",\"PAGO TARJETA BAC\",\"PAGOTARJETABAC\",\"SEMANA MAXIPAL\",\"PAGO SERVICIO PROFESIONAL\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"RETIRO ATM\",\"RETIRO CAJERO\",\"RETIRO EFECTIVO\"],\n                                            \"idBankMovementType\":5,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO TARJET\",\"PAGO TC\",\"TARJETA CRED\"],\n                                            \"idBankMovementType\":6,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO PREST\",\"CUOTA PREST\",\"PAGO PRESTAMO\",\"CUOTA PRESTAMO\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"SINPE MOVIL DEB\",\"DEB SINPE\",\"CARGO SINPE\",\"TRANSF DEB\"],\n                                            \"idBankMovementType\":8,\"matchMode\":\"Any\"}\n]");
        }
    }
}
