using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBacTemplateKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 4,
                column: "keywordRules",
                value: "[\n  {\"keywords\":[\"SU PAGO RECIBIDO GRACIAS\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"UBER\",\"DLC*UBER\",\"DLC*LYFT\",\"BOLT\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"APPLE.COM\",\"NETFLIX.COM\",\"GITHUB\",\"SPOTIFY\",\"YOUTUBE\",\"AMAZON\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"WALMART\",\"MAXIPALI\",\"MXM \",\"SUPER SALON\",\"AUTOMERCADO\",\"PALI \",\"SIMAN\",\"ALMACENES\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"FARMACIA\",\"DROGUERIA\",\"CLINICA \",\"HOSPITAL\",\"OPTICA \",\"LABORATORIO\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"FERRETERIA\",\"DEPOSITO FERR\",\"CONSTRUPLAZA\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"GOOGLE\",\"MICROSOFT\",\"2CO.COM\",\"OPENAI\",\"CHATGPT\",\"DIGITALOCEAN\",\"NEOTHEK\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"SEGURO PROTECCION\",\"SEGURO DE VIDA\",\"PRIMA SEGURO\",\"INS \"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"IVA -\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRASLADO SALDO REVOLUTIVO\",\"CUOTA:\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"}\n]");

            migrationBuilder.UpdateData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 5,
                column: "keywordRules",
                value: "[\n  {\"keywords\":[\"SU PAGO RECIBIDO GRACIAS\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"UBER\",\"DLC*UBER\",\"DLC*LYFT\",\"BOLT\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"APPLE.COM\",\"NETFLIX.COM\",\"GITHUB\",\"SPOTIFY\",\"YOUTUBE\",\"AMAZON\",\"JETBRAINS\",\"GOOGLE\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"GAMMA.APP\",\"OPENAI\",\"CHATGPT\",\"MICROSOFT\",\"DIGITALOCEAN\",\"2CO.COM\",\"NEOTHEK\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"ICON CC RETAIL\",\"WALMART\",\"AMAZON\",\"SIMAN\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"SEGURO PROTECCION\",\"SEGURO DE VIDA\",\"PRIMA SEGURO\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"IVA -\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRASLADO SALDO REVOLUTIVO\",\"CUOTA:\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"}\n]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 4,
                column: "keywordRules",
                value: "[\n  {\"keywords\":[\"SU PAGO RECIBIDO GRACIAS\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"UBER\",\"DLC*UBER\",\"DLC*LYFT\",\"BOLT\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"APPLE.COM\",\"NETFLIX.COM\",\"GITHUB\",\"SPOTIFY\",\"YOUTUBE\",\"AMAZON\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"WALMART\",\"MAXIPALI\",\"MXM \",\"SUPER SALON\",\"AUTOMERCADO\",\"PALI \"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"IVA -\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRASLADO SALDO REVOLUTIVO\",\"CUOTA:\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"}\n]");

            migrationBuilder.UpdateData(
                table: "bankStatementTemplate",
                keyColumn: "idBankStatementTemplate",
                keyValue: 5,
                column: "keywordRules",
                value: "[\n  {\"keywords\":[\"SU PAGO RECIBIDO GRACIAS\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"UBER\",\"DLC*UBER\",\"DLC*LYFT\",\"BOLT\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"APPLE.COM\",\"NETFLIX.COM\",\"GITHUB\",\"SPOTIFY\",\"YOUTUBE\",\"AMAZON\",\"JETBRAINS\",\"GOOGLE\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"IVA -\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRASLADO SALDO REVOLUTIVO\",\"CUOTA:\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"}\n]");
        }
    }
}
