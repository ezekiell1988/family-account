using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyAndExchangeRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currency",
                columns: table => new
                {
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la moneda.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeCurrency = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, comment: "Código único de la moneda según estándar internacional. Ejemplo: CRC, USD, EUR."),
                    nameCurrency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nombre descriptivo de la moneda. Ejemplo: Colón costarricense, Dólar estadounidense."),
                    symbolCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, comment: "Símbolo representativo de la moneda. Ejemplo: ₡, $, €.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency", x => x.idCurrency);
                },
                comment: "Monedas disponibles en el sistema contable para registrar operaciones y tipos de cambio.");

            migrationBuilder.CreateTable(
                name: "exchangeRate",
                columns: table => new
                {
                    idExchangeRate = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de cambio.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda a la que pertenece este tipo de cambio."),
                    rateDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha efectiva del tipo de cambio."),
                    rateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Valor del tipo de cambio de la moneda respecto a la moneda base definida por la organización.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchangeRate", x => x.idExchangeRate);
                    table.CheckConstraint("CK_exchangeRate_rateValue_positive", "rateValue > 0");
                    table.ForeignKey(
                        name: "FK_exchangeRate_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Tipos de cambio por moneda y fecha para soportar operaciones multi-moneda en el sistema contable.");

            migrationBuilder.InsertData(
                table: "currency",
                columns: new[] { "idCurrency", "codeCurrency", "nameCurrency", "symbolCurrency" },
                values: new object[,]
                {
                    { 1, "CRC", "Colón costarricense", "₡" },
                    { 2, "USD", "Dólar estadounidense", "$" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_currency_codeCurrency",
                table: "currency",
                column: "codeCurrency",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exchangeRate_idCurrency",
                table: "exchangeRate",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "UQ_exchangeRate_idCurrency_rateDate",
                table: "exchangeRate",
                columns: new[] { "idCurrency", "rateDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exchangeRate");

            migrationBuilder.DropTable(
                name: "currency");
        }
    }
}
