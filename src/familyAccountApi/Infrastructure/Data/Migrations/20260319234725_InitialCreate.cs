using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    idRole = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    nameRole = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    descriptionRole = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.idRole);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    idUser = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    codeUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nameUser = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    phoneUser = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    emailUser = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.idUser);
                });

            migrationBuilder.CreateTable(
                name: "userPin",
                columns: table => new
                {
                    idUserPin = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idUser = table.Column<int>(type: "int", nullable: false),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    pin = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userPin", x => x.idUserPin);
                    table.ForeignKey(
                        name: "FK_userPin_user_idUser",
                        column: x => x.idUser,
                        principalTable: "user",
                        principalColumn: "idUser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userRole",
                columns: table => new
                {
                    idUserRole = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idUser = table.Column<int>(type: "int", nullable: false),
                    idRole = table.Column<int>(type: "int", nullable: false),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userRole", x => x.idUserRole);
                    table.ForeignKey(
                        name: "FK_userRole_role_idRole",
                        column: x => x.idRole,
                        principalTable: "role",
                        principalColumn: "idRole",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_userRole_user_idUser",
                        column: x => x.idUser,
                        principalTable: "user",
                        principalColumn: "idUser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "role",
                columns: new[] { "idRole", "createAt", "descriptionRole", "nameRole" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "Acceso total al sistema", "Developer" },
                    { 2, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "Administrador con acceso amplio", "Admin" },
                    { 3, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "Usuario estándar", "User" }
                });

            migrationBuilder.InsertData(
                table: "user",
                columns: new[] { "idUser", "codeUser", "createAt", "emailUser", "nameUser", "phoneUser" },
                values: new object[] { 1, "S", new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "ezekiell1988@hotmail.com", "Ezequiel Baltodano Cubillo", "50683681485" });

            migrationBuilder.InsertData(
                table: "userRole",
                columns: new[] { "idUserRole", "createAt", "idRole", "idUser" },
                values: new object[] { 1, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 1 });

            migrationBuilder.CreateIndex(
                name: "UQ_role_nameRole",
                table: "role",
                column: "nameRole",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_user_codeUser",
                table: "user",
                column: "codeUser",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_userPin_idUser_pin",
                table: "userPin",
                columns: new[] { "idUser", "pin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_userRole_idRole",
                table: "userRole",
                column: "idRole");

            migrationBuilder.CreateIndex(
                name: "UQ_userRole_idUser_idRole",
                table: "userRole",
                columns: new[] { "idUser", "idRole" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "userPin");

            migrationBuilder.DropTable(
                name: "userRole");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
