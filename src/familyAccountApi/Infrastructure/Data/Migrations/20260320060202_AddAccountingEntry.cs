using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accountingEntry",
                columns: table => new
                {
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del asiento contable.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que pertenece el asiento contable."),
                    numberEntry = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, comment: "Número o consecutivo del asiento contable dentro del período fiscal."),
                    dateEntry = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha contable del asiento."),
                    descriptionEntry = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false, comment: "Descripción general del asiento contable."),
                    statusEntry = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Estado del asiento contable: Borrador | Publicado | Anulado."),
                    referenceEntry = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true, comment: "Referencia opcional del asiento, como número de documento, factura o comprobante externo."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()", comment: "Fecha y hora de creación del asiento contable.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accountingEntry", x => x.idAccountingEntry);
                    table.CheckConstraint("CK_accountingEntry_statusEntry", "statusEntry IN ('Borrador', 'Publicado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_accountingEntry_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Cabecera del asiento contable. Agrupa las líneas de débito y crédito registradas dentro de un período fiscal determinado.");

            migrationBuilder.CreateTable(
                name: "accountingEntryLine",
                columns: table => new
                {
                    idAccountingEntryLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea del asiento contable.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false, comment: "FK al asiento contable al que pertenece la línea."),
                    idAccount = table.Column<int>(type: "int", nullable: false, comment: "FK a la cuenta contable afectada por esta línea."),
                    debitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto registrado al débito. Debe ser mayor que cero solo cuando la línea es de débito."),
                    creditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto registrado al crédito. Debe ser mayor que cero solo cuando la línea es de crédito."),
                    descriptionLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "Descripción opcional y específica de la línea del asiento contable.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accountingEntryLine", x => x.idAccountingEntryLine);
                    table.CheckConstraint("CK_accountingEntryLine_singleSidedAmount", "((debitAmount > 0 AND creditAmount = 0) OR (debitAmount = 0 AND creditAmount > 0))");
                    table.ForeignKey(
                        name: "FK_accountingEntryLine_account_idAccount",
                        column: x => x.idAccount,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accountingEntryLine_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Líneas del asiento contable. Cada línea afecta una cuenta contable con un monto al débito o al crédito.");

            migrationBuilder.CreateIndex(
                name: "UQ_accountingEntry_idFiscalPeriod_numberEntry",
                table: "accountingEntry",
                columns: new[] { "idFiscalPeriod", "numberEntry" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accountingEntryLine_idAccount",
                table: "accountingEntryLine",
                column: "idAccount");

            migrationBuilder.CreateIndex(
                name: "IX_accountingEntryLine_idAccountingEntry",
                table: "accountingEntryLine",
                column: "idAccountingEntry");

            migrationBuilder.Sql(
                """
                CREATE TRIGGER TR_accountingEntry_ValidatePublishedBalance
                ON accountingEntry
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted i
                        OUTER APPLY
                        (
                            SELECT
                                COUNT(*) AS lineCount,
                                SUM(ael.debitAmount) AS totalDebit,
                                SUM(ael.creditAmount) AS totalCredit
                            FROM accountingEntryLine ael
                            WHERE ael.idAccountingEntry = i.idAccountingEntry
                        ) totals
                        WHERE i.statusEntry = 'Publicado'
                          AND (
                                ISNULL(totals.lineCount, 0) = 0
                                OR ISNULL(totals.totalDebit, 0) <> ISNULL(totals.totalCredit, 0)
                              )
                    )
                    BEGIN
                        THROW 50001, 'El asiento publicado debe estar balanceado: la suma del débito debe ser igual a la suma del crédito.', 1;
                    END
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER TR_accountingEntryLine_ValidatePublishedBalance
                ON accountingEntryLine
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM accountingEntry ae
                        INNER JOIN
                        (
                            SELECT idAccountingEntry FROM inserted
                            UNION
                            SELECT idAccountingEntry FROM deleted
                        ) affected
                            ON affected.idAccountingEntry = ae.idAccountingEntry
                        OUTER APPLY
                        (
                            SELECT
                                COUNT(*) AS lineCount,
                                SUM(ael.debitAmount) AS totalDebit,
                                SUM(ael.creditAmount) AS totalCredit
                            FROM accountingEntryLine ael
                            WHERE ael.idAccountingEntry = ae.idAccountingEntry
                        ) totals
                        WHERE ae.statusEntry = 'Publicado'
                          AND (
                                ISNULL(totals.lineCount, 0) = 0
                                OR ISNULL(totals.totalDebit, 0) <> ISNULL(totals.totalCredit, 0)
                              )
                    )
                    BEGIN
                        THROW 50002, 'No se puede dejar desbalanceado un asiento publicado.', 1;
                    END
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID('TR_accountingEntryLine_ValidatePublishedBalance', 'TR') IS NOT NULL
                    DROP TRIGGER TR_accountingEntryLine_ValidatePublishedBalance;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID('TR_accountingEntry_ValidatePublishedBalance', 'TR') IS NOT NULL
                    DROP TRIGGER TR_accountingEntry_ValidatePublishedBalance;
                """);

            migrationBuilder.DropTable(
                name: "accountingEntryLine");

            migrationBuilder.DropTable(
                name: "accountingEntry");
        }
    }
}
