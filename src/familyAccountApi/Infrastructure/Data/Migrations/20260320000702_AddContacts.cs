using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact",
                columns: table => new
                {
                    idContact = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeContact = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact", x => x.idContact);
                });

            migrationBuilder.CreateTable(
                name: "contactType",
                columns: table => new
                {
                    idContactType = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeContactType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contactType", x => x.idContactType);
                });

            migrationBuilder.CreateTable(
                name: "contactContactType",
                columns: table => new
                {
                    idContactContactType = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idContact = table.Column<int>(type: "int", nullable: false),
                    idContactType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contactContactType", x => x.idContactContactType);
                    table.ForeignKey(
                        name: "FK_contactContactType_contactType_idContactType",
                        column: x => x.idContactType,
                        principalTable: "contactType",
                        principalColumn: "idContactType",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contactContactType_contact_idContact",
                        column: x => x.idContact,
                        principalTable: "contact",
                        principalColumn: "idContact",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "contactType",
                columns: new[] { "idContactType", "codeContactType", "name" },
                values: new object[,]
                {
                    { 1, "CLI", "Cliente" },
                    { 2, "PRO", "Proveedor" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_contact_codeContact",
                table: "contact",
                column: "codeContact",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contactContactType_idContactType",
                table: "contactContactType",
                column: "idContactType");

            migrationBuilder.CreateIndex(
                name: "UQ_contactContactType_idContact_idContactType",
                table: "contactContactType",
                columns: new[] { "idContact", "idContactType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_contactType_codeContactType",
                table: "contactType",
                column: "codeContactType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contactContactType");

            migrationBuilder.DropTable(
                name: "contactType");

            migrationBuilder.DropTable(
                name: "contact");
        }
    }
}
