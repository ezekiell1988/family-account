using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetBankAccountAndAccountingEntryCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "exchangeRateValue",
                table: "accountingEntry",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                comment: "Tipo de cambio utilizado al momento de registrar el asiento contable.");

            migrationBuilder.AddColumn<int>(
                name: "idCurrency",
                table: "accountingEntry",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "FK a la moneda en la que fue registrado el asiento contable.");

            migrationBuilder.CreateTable(
                name: "bankAccount",
                columns: table => new
                {
                    idBankAccount = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la cuenta bancaria.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idAccount = table.Column<int>(type: "int", nullable: false, comment: "FK a la cuenta contable que representa esta cuenta bancaria en el mayor."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda manejada por la cuenta bancaria."),
                    codeBankAccount = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código interno único de la cuenta bancaria."),
                    bankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre de la entidad bancaria."),
                    accountNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número de cuenta bancaria o IBAN."),
                    accountHolder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Titular de la cuenta bancaria."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si la cuenta bancaria está activa para operaciones y conciliaciones.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankAccount", x => x.idBankAccount);
                    table.ForeignKey(
                        name: "FK_bankAccount_account_idAccount",
                        column: x => x.idAccount,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bankAccount_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Cuentas bancarias vinculadas a cuentas contables y monedas para conciliación y control de efectivo.");

            migrationBuilder.CreateTable(
                name: "budget",
                columns: table => new
                {
                    idBudget = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del presupuesto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idAccount = table.Column<int>(type: "int", nullable: false, comment: "FK a la cuenta contable asociada al presupuesto."),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que aplica el presupuesto."),
                    amountBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto presupuestado para la cuenta contable en el período fiscal indicado."),
                    notesBudget = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "Notas u observaciones opcionales del presupuesto."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si el presupuesto está activo para control y consulta.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget", x => x.idBudget);
                    table.CheckConstraint("CK_budget_amountBudget_positive", "amountBudget > 0");
                    table.ForeignKey(
                        name: "FK_budget_account_idAccount",
                        column: x => x.idAccount,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Presupuestos contables por cuenta y período fiscal para control y análisis financiero.");

            migrationBuilder.CreateIndex(
                name: "IX_accountingEntry_idCurrency",
                table: "accountingEntry",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_bankAccount_idAccount",
                table: "bankAccount",
                column: "idAccount");

            migrationBuilder.CreateIndex(
                name: "IX_bankAccount_idCurrency",
                table: "bankAccount",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "UQ_bankAccount_accountNumber",
                table: "bankAccount",
                column: "accountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_bankAccount_codeBankAccount",
                table: "bankAccount",
                column: "codeBankAccount",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_budget_idFiscalPeriod",
                table: "budget",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "UQ_budget_idAccount_idFiscalPeriod",
                table: "budget",
                columns: new[] { "idAccount", "idFiscalPeriod" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_accountingEntry_currency_idCurrency",
                table: "accountingEntry",
                column: "idCurrency",
                principalTable: "currency",
                principalColumn: "idCurrency",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accountingEntry_currency_idCurrency",
                table: "accountingEntry");

            migrationBuilder.DropTable(
                name: "bankAccount");

            migrationBuilder.DropTable(
                name: "budget");

            migrationBuilder.DropIndex(
                name: "IX_accountingEntry_idCurrency",
                table: "accountingEntry");

            migrationBuilder.DropColumn(
                name: "exchangeRateValue",
                table: "accountingEntry");

            migrationBuilder.DropColumn(
                name: "idCurrency",
                table: "accountingEntry");
        }
    }
}
