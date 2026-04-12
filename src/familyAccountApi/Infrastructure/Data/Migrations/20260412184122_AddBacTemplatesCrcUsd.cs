using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBacTemplatesCrcUsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "bankStatementTemplate",
                columns: new[] { "idBankStatementTemplate", "bankName", "codeTemplate", "columnMappings", "dateFormat", "isActive", "keywordRules", "nameTemplate", "notes", "timeFormat" },
                values: new object[,]
                {
                    { 4, "BAC Credomatic", "BAC-TXT-CRC-V1", "{\"currency\":\"CRC\"}", "dd/MM/yyyy", true, "[\n  {\"keywords\":[\"SU PAGO RECIBIDO GRACIAS\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"UBER\",\"DLC*UBER\",\"DLC*LYFT\",\"BOLT\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"APPLE.COM\",\"NETFLIX.COM\",\"GITHUB\",\"SPOTIFY\",\"YOUTUBE\",\"AMAZON\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"WALMART\",\"MAXIPALI\",\"MXM \",\"SUPER SALON\",\"AUTOMERCADO\",\"PALI \"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"IVA -\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRASLADO SALDO REVOLUTIVO\",\"CUOTA:\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"}\n]", "BAC Credomatic – Tarjeta Crédito CRC (TXT)", "Archivo .txt pipe-delimitado exportado desde el portal BAC. Sólo se procesa la columna Local (CRC). Usar para archivos *-CRC.txt de tarjetas de crédito en colones.", null },
                    { 5, "BAC Credomatic", "BAC-TXT-USD-V1", "{\"currency\":\"USD\"}", "dd/MM/yyyy", true, "[\n  {\"keywords\":[\"SU PAGO RECIBIDO GRACIAS\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"UBER\",\"DLC*UBER\",\"DLC*LYFT\",\"BOLT\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"APPLE.COM\",\"NETFLIX.COM\",\"GITHUB\",\"SPOTIFY\",\"YOUTUBE\",\"AMAZON\",\"JETBRAINS\",\"GOOGLE\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"IVA -\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRASLADO SALDO REVOLUTIVO\",\"CUOTA:\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"}\n]", "BAC Credomatic – Tarjeta Crédito USD (TXT)", "Archivo .txt pipe-delimitado exportado desde el portal BAC. Sólo se procesa la columna Dollars (USD). Usar para archivos *-USD.txt de tarjetas de crédito en dólares.", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 5);
        }
    }
}
