using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    idUser = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "userPin");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
