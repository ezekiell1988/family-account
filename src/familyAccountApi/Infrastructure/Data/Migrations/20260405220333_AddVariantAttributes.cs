using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyAccountApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isVariantParent",
                table: "product",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "Indica que el producto es un padre que agrupa variantes por atributos (talla, color, etc.). Los padres no tienen stock propio.");

            migrationBuilder.CreateTable(
                name: "productAttribute",
                columns: table => new
                {
                    idProductAttribute = table.Column<int>(type: "int", nullable: false, comment: "Identificador único del atributo del producto")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Producto padre al que pertenece este atributo"),
                    nameAttribute = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nombre del atributo (ej: Talla, Color, Material)"),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación del atributo dentro del producto padre")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productAttribute", x => x.idProductAttribute);
                    table.ForeignKey(
                        name: "FK_productAttribute_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Atributos definibles por producto padre que describen dimensiones de variación (ej: Talla, Color)");

            migrationBuilder.CreateTable(
                name: "attributeValue",
                columns: table => new
                {
                    idAttributeValue = table.Column<int>(type: "int", nullable: false, comment: "Identificador único del valor de atributo")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductAttribute = table.Column<int>(type: "int", nullable: false, comment: "Atributo al que pertenece este valor"),
                    nameValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nombre del valor (ej: S, M, L, Azul, Rojo)"),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación del valor dentro del atributo")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attributeValue", x => x.idAttributeValue);
                    table.ForeignKey(
                        name: "FK_attributeValue_productAttribute_idProductAttribute",
                        column: x => x.idProductAttribute,
                        principalTable: "productAttribute",
                        principalColumn: "idProductAttribute",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Valores posibles para cada atributo de producto padre (ej: S, M, L para el atributo Talla)");

            migrationBuilder.CreateTable(
                name: "productVariantAttribute",
                columns: table => new
                {
                    idProductVariantAttribute = table.Column<int>(type: "int", nullable: false, comment: "Identificador único del vínculo variante-atributo")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Producto variante hijo al que pertenece este vínculo"),
                    idAttributeValue = table.Column<int>(type: "int", nullable: false, comment: "Valor de atributo que forma parte de la combinación de esta variante")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productVariantAttribute", x => x.idProductVariantAttribute);
                    table.ForeignKey(
                        name: "FK_productVariantAttribute_attributeValue_idAttributeValue",
                        column: x => x.idAttributeValue,
                        principalTable: "attributeValue",
                        principalColumn: "idAttributeValue",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productVariantAttribute_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Vincula una variante hija con los valores de atributo que la definen (ej: Camisa Oxford M + Azul)");

            migrationBuilder.CreateIndex(
                name: "UQ_attributeValue_idProductAttribute_nameValue",
                table: "attributeValue",
                columns: new[] { "idProductAttribute", "nameValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_productAttribute_idProduct_nameAttribute",
                table: "productAttribute",
                columns: new[] { "idProduct", "nameAttribute" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productVariantAttribute_idAttributeValue",
                table: "productVariantAttribute",
                column: "idAttributeValue");

            migrationBuilder.CreateIndex(
                name: "UQ_productVariantAttribute_idProduct_idAttributeValue",
                table: "productVariantAttribute",
                columns: new[] { "idProduct", "idAttributeValue" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "productVariantAttribute");

            migrationBuilder.DropTable(
                name: "attributeValue");

            migrationBuilder.DropTable(
                name: "productAttribute");

            migrationBuilder.DropColumn(
                name: "isVariantParent",
                table: "product");
        }
    }
}
