using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankStatementImportSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bankStatementTemplate",
                columns: table => new
                {
                    idBankStatementTemplate = table.Column<int>(type: "int", nullable: false, comment: "Identificador único de la plantilla")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeTemplate = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código único de la plantilla (p. ej. BCR-CHECKING-2024)"),
                    nameTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre descriptivo de la plantilla"),
                    bankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre del banco emisor del extracto"),
                    columnMappings = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "Mapeo de columnas en formato JSON con índices y nombres de campos del Excel"),
                    dateFormat = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, comment: "Formato de fecha usado en el Excel (p. ej. dd/MM/yyyy)"),
                    timeFormat = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, comment: "Formato de hora usado en el Excel (p. ej. HH:mm)"),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si la plantilla está activa para uso"),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Notas o instrucciones adicionales para el uso de la plantilla")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankStatementTemplate", x => x.idBankStatementTemplate);
                },
                comment: "Plantillas de carga para extractos bancarios por entidad financiera");

            migrationBuilder.CreateTable(
                name: "bankStatementImport",
                columns: table => new
                {
                    idBankStatementImport = table.Column<int>(type: "int", nullable: false, comment: "Identificador único de la importación")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idBankAccount = table.Column<int>(type: "int", nullable: false, comment: "Cuenta bancaria asociada a la importación"),
                    idBankStatementTemplate = table.Column<int>(type: "int", nullable: false, comment: "Plantilla utilizada para procesar el extracto"),
                    fileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "Nombre del archivo Excel importado"),
                    importDate = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha y hora de la importación"),
                    importedBy = table.Column<int>(type: "int", nullable: false, comment: "Usuario que realizó la importación"),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending", comment: "Estado de la importación: Pending, Processing, Completed, Failed"),
                    totalTransactions = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Número total de transacciones en el archivo"),
                    processedTransactions = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Número de transacciones procesadas exitosamente"),
                    errorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "Mensaje de error en caso de fallo en la importación")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankStatementImport", x => x.idBankStatementImport);
                    table.CheckConstraint("CK_bankStatementImport_status", "status IN ('Pending', 'Processing', 'Completed', 'Failed')");
                    table.ForeignKey(
                        name: "FK_bankStatementImport_bankAccount_idBankAccount",
                        column: x => x.idBankAccount,
                        principalTable: "bankAccount",
                        principalColumn: "idBankAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bankStatementImport_bankStatementTemplate_idBankStatementTemplate",
                        column: x => x.idBankStatementTemplate,
                        principalTable: "bankStatementTemplate",
                        principalColumn: "idBankStatementTemplate",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bankStatementImport_user_importedBy",
                        column: x => x.importedBy,
                        principalTable: "user",
                        principalColumn: "idUser",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Registro de importaciones de extractos bancarios");

            migrationBuilder.CreateTable(
                name: "bankStatementTransaction",
                columns: table => new
                {
                    idBankStatementTransaction = table.Column<int>(type: "int", nullable: false, comment: "Identificador único de la transacción del extracto")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idBankStatementImport = table.Column<int>(type: "int", nullable: false, comment: "Importación a la que pertenece esta transacción"),
                    accountingDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha contable de la transacción según el banco"),
                    transactionDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha real de ejecución de la transacción"),
                    transactionTime = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Hora de la transacción si está disponible"),
                    documentNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true, comment: "Número de documento o referencia de la transacción"),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "Descripción de la transacción proporcionada por el banco"),
                    debitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true, comment: "Monto de débito (retiro o pago)"),
                    creditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true, comment: "Monto de crédito (depósito o ingreso)"),
                    balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true, comment: "Saldo resultante después de la transacción"),
                    isReconciled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Indica si la transacción ha sido conciliada con un asiento contable"),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: true, comment: "Asiento contable asociado para conciliación (opcional)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankStatementTransaction", x => x.idBankStatementTransaction);
                    table.ForeignKey(
                        name: "FK_bankStatementTransaction_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bankStatementTransaction_bankStatementImport_idBankStatementImport",
                        column: x => x.idBankStatementImport,
                        principalTable: "bankStatementImport",
                        principalColumn: "idBankStatementImport",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Transacciones individuales importadas de extractos bancarios");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementImport_idBankAccount",
                table: "bankStatementImport",
                column: "idBankAccount");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementImport_idBankStatementTemplate",
                table: "bankStatementImport",
                column: "idBankStatementTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementImport_importDate",
                table: "bankStatementImport",
                column: "importDate");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementImport_importedBy",
                table: "bankStatementImport",
                column: "importedBy");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementImport_status",
                table: "bankStatementImport",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "UQ_bankStatementTemplate_codeTemplate",
                table: "bankStatementTemplate",
                column: "codeTemplate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementTransaction_accountingDate",
                table: "bankStatementTransaction",
                column: "accountingDate");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementTransaction_idAccountingEntry",
                table: "bankStatementTransaction",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementTransaction_idBankStatementImport",
                table: "bankStatementTransaction",
                column: "idBankStatementImport");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementTransaction_isReconciled",
                table: "bankStatementTransaction",
                column: "isReconciled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bankStatementTransaction");

            migrationBuilder.DropTable(
                name: "bankStatementImport");

            migrationBuilder.DropTable(
                name: "bankStatementTemplate");
        }
    }
}
