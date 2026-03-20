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
                name: "account",
                columns: table => new
                {
                    idAccount = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la cuenta contable.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeAccount = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Código jerárquico único de la cuenta. Ej: '1', '1.1', '1.1.01'."),
                    nameAccount = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre descriptivo de la cuenta contable."),
                    typeAccount = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Tipo contable: Activo | Pasivo | Capital | Ingreso | Gasto | Control."),
                    levelAccount = table.Column<int>(type: "int", nullable: false, comment: "Nivel jerárquico dentro del plan de cuentas. 1 = cuenta raíz."),
                    idAccountParent = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta padre. NULL indica que es una cuenta raíz."),
                    allowsMovements = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si la cuenta acepta asientos contables directos (true) o es solo agrupadora (false)."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si la cuenta está activa y disponible para su uso.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.idAccount);
                    table.CheckConstraint("CK_account_typeAccount", "typeAccount IN ('Activo', 'Pasivo', 'Capital', 'Ingreso', 'Gasto', 'Control')");
                    table.ForeignKey(
                        name: "FK_account_account_idAccountParent",
                        column: x => x.idAccountParent,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo de cuentas contables con jerarquía auto-referenciada. Permite registrar el plan de cuentas con padres e hijos. typeAccount: Activo, Pasivo, Capital, Ingreso, Gasto, Control.");

            migrationBuilder.CreateTable(
                name: "contact",
                columns: table => new
                {
                    idContact = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del contacto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeContact = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código único de identificación del contacto. Usado internamente para referencias rápidas."),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre completo o razón social del contacto.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact", x => x.idContact);
                },
                comment: "Catálogo de contactos del sistema: clientes, proveedores u otras entidades externas. Cada contacto puede tener uno o más tipos asignados a través de la tabla contactContactType.");

            migrationBuilder.CreateTable(
                name: "contactType",
                columns: table => new
                {
                    idContactType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de contacto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeContactType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código abreviado único del tipo de contacto. Ej: 'CLI' (Cliente), 'PRO' (Proveedor)."),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre descriptivo del tipo de contacto. Ej: 'Cliente', 'Proveedor'.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contactType", x => x.idContactType);
                },
                comment: "Catálogo de tipos de contacto. Permite clasificar un contacto con una o más categorías (ej: Cliente, Proveedor). Los valores se asignan a contactos a través de la tabla contactContactType.");

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del producto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeProduct = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código interno único del producto. Definido por la empresa, distinto al código de barras del fabricante."),
                    nameProduct = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre interno del producto usado en el sistema.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.idProduct);
                },
                comment: "Catálogo interno de productos de la empresa. Un producto puede estar relacionado con uno o más SKUs escaneables (productProductSKU) y pertenecer a una o más categorías (productProductCategory).");

            migrationBuilder.CreateTable(
                name: "productCategory",
                columns: table => new
                {
                    idProductCategory = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la categoría de producto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nameProductCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nombre descriptivo de la categoría. Ej: 'Lácteos', 'Limpieza', 'Bebidas'.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productCategory", x => x.idProductCategory);
                },
                comment: "Catálogo de categorías de productos. Permite agrupar y clasificar los productos internos para facilitar su búsqueda y organización. Un producto puede pertenecer a múltiples categorías a través de productProductCategory.");

            migrationBuilder.CreateTable(
                name: "productSKU",
                columns: table => new
                {
                    idProductSKU = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del SKU.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeProductSKU = table.Column<string>(type: "varchar(48)", unicode: false, maxLength: 48, nullable: false, comment: "Código de barras del producto. Soporta EAN-8 (8 dígitos), EAN-13 (13 dígitos), UPC-A (12 dígitos) y otros formatos de hasta 48 caracteres."),
                    nameProductSKU = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre completo del producto tal como aparece en el empaque."),
                    brandProductSKU = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Marca o fabricante del producto. Ej: 'Nestlé', 'Unilever'."),
                    descriptionProductSKU = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Descripción detallada del producto: ingredientes, características, uso recomendado."),
                    netContent = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, comment: "Contenido neto del producto con su unidad de medida. Ej: '500ml', '1kg', '12 unidades'.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productSKU", x => x.idProductSKU);
                },
                comment: "Catálogo de SKUs de productos identificados por código de barras. Un SKU representa la unidad comercial exacta de un producto (marca + contenido + presentación). Múltiples productos internos pueden referenciar el mismo SKU a través de productProductSKU.");

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    idRole = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del rol.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()", comment: "Fecha y hora de creación del rol."),
                    nameRole = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Nombre único del rol. Valores del sistema: Developer, Admin, User."),
                    descriptionRole = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true, comment: "Descripción del nivel de acceso y permisos que otorga el rol.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.idRole);
                },
                comment: "Roles de acceso del sistema. Define los niveles de autorización: Developer (acceso total), Admin (acceso amplio), User (acceso básico). Los roles se asignan a usuarios a través de la tabla userRole.");

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    idUser = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del usuario.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()", comment: "Fecha y hora de creación del registro. Se asigna automáticamente a la fecha actual del servidor."),
                    codeUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Código único de identificación del usuario. Usado como nombre de usuario en el login."),
                    nameUser = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre completo del usuario."),
                    phoneUser = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, comment: "Número de teléfono del usuario (opcional). Formato libre, ej: '50683681485'."),
                    emailUser = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false, comment: "Correo electrónico del usuario. Usado para notificaciones y envío de PIN.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.idUser);
                },
                comment: "Usuarios del sistema que pueden autenticarse con JWT. Cada usuario puede tener uno o más roles (Developer, Admin, User).");

            migrationBuilder.CreateTable(
                name: "contactContactType",
                columns: table => new
                {
                    idContactContactType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la asociación contacto-tipo.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idContact = table.Column<int>(type: "int", nullable: false, comment: "FK al contacto."),
                    idContactType = table.Column<int>(type: "int", nullable: false, comment: "FK al tipo de contacto.")
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
                },
                comment: "Tabla de asociación muchos-a-muchos entre contactos y tipos de contacto. Permite que un mismo contacto sea clasificado como Cliente, Proveedor u otros tipos simultáneamente. No se permite la misma combinación contacto-tipo dos veces.");

            migrationBuilder.CreateTable(
                name: "productProductCategory",
                columns: table => new
                {
                    idProductProductCategory = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la asociación producto-categoría.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto interno."),
                    idProductCategory = table.Column<int>(type: "int", nullable: false, comment: "FK a la categoría de producto.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productProductCategory", x => x.idProductProductCategory);
                    table.ForeignKey(
                        name: "FK_productProductCategory_productCategory_idProductCategory",
                        column: x => x.idProductCategory,
                        principalTable: "productCategory",
                        principalColumn: "idProductCategory",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productProductCategory_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla de asociación muchos-a-muchos entre productos internos y categorías. Permite que un producto pertenezca a múltiples categorías y que una categoría agrupe múltiples productos. No se permite la misma combinación producto-categoría dos veces.");

            migrationBuilder.CreateTable(
                name: "productProductSKU",
                columns: table => new
                {
                    idProductProductSKU = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la asociación producto-SKU.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto interno."),
                    idProductSKU = table.Column<int>(type: "int", nullable: false, comment: "FK al SKU de código de barras.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productProductSKU", x => x.idProductProductSKU);
                    table.ForeignKey(
                        name: "FK_productProductSKU_productSKU_idProductSKU",
                        column: x => x.idProductSKU,
                        principalTable: "productSKU",
                        principalColumn: "idProductSKU",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productProductSKU_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla de asociación muchos-a-muchos entre productos internos y SKUs de código de barras. Permite que un producto interno (product) esté vinculado a múltiples SKUs escaneables y que un mismo SKU pueda usarse en varios productos.");

            migrationBuilder.CreateTable(
                name: "userPin",
                columns: table => new
                {
                    idUserPin = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del registro de PIN.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idUser = table.Column<int>(type: "int", nullable: false, comment: "FK al usuario propietario del PIN."),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()", comment: "Fecha y hora en que se generó el PIN. Se usa para validar su vigencia."),
                    pin = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false, comment: "PIN numérico de 5 dígitos generado aleatoriamente y enviado al usuario por correo.")
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
                },
                comment: "PINs temporales de 5 dígitos usados para autenticación de dos factores. Se generan por solicitud y se envían al correo del usuario. Un PIN no puede repetirse para el mismo usuario hasta que expire o sea usado.");

            migrationBuilder.CreateTable(
                name: "userRole",
                columns: table => new
                {
                    idUserRole = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la asignación usuario-rol.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idUser = table.Column<int>(type: "int", nullable: false, comment: "FK al usuario al que se le asigna el rol."),
                    idRole = table.Column<int>(type: "int", nullable: false, comment: "FK al rol que se le asigna al usuario."),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()", comment: "Fecha y hora en que se asignó el rol al usuario.")
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
                },
                comment: "Tabla de asociación muchos-a-muchos entre usuarios y roles. Un usuario puede tener múltiples roles y un rol puede asignarse a múltiples usuarios. No se permite asignar el mismo rol dos veces al mismo usuario.");

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[,]
                {
                    { 1, false, "1", null, true, 1, "Activo", "Activo" },
                    { 2, false, "2", null, true, 1, "Pasivo", "Pasivo" },
                    { 3, false, "3", null, true, 1, "Capital", "Capital" },
                    { 4, false, "4", null, true, 1, "Ingreso", "Ingreso" },
                    { 5, false, "5", null, true, 1, "Gasto", "Gasto" },
                    { 6, false, "6", null, true, 1, "Control", "Control" }
                });

            migrationBuilder.InsertData(
                table: "contactType",
                columns: new[] { "idContactType", "codeContactType", "name" },
                values: new object[,]
                {
                    { 1, "CLI", "Cliente" },
                    { 2, "PRO", "Proveedor" }
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
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 1 },
                    { 2, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 1 },
                    { 3, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_idAccountParent",
                table: "account",
                column: "idAccountParent");

            migrationBuilder.CreateIndex(
                name: "UQ_account_codeAccount",
                table: "account",
                column: "codeAccount",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "UQ_product_codeProduct",
                table: "product",
                column: "codeProduct",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productProductCategory_idProductCategory",
                table: "productProductCategory",
                column: "idProductCategory");

            migrationBuilder.CreateIndex(
                name: "UQ_productProductCategory_idProduct_idProductCategory",
                table: "productProductCategory",
                columns: new[] { "idProduct", "idProductCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productProductSKU_idProductSKU",
                table: "productProductSKU",
                column: "idProductSKU");

            migrationBuilder.CreateIndex(
                name: "UQ_productProductSKU_idProduct_idProductSKU",
                table: "productProductSKU",
                columns: new[] { "idProduct", "idProductSKU" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_productSKU_codeProductSKU",
                table: "productSKU",
                column: "codeProductSKU",
                unique: true);

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
                name: "account");

            migrationBuilder.DropTable(
                name: "contactContactType");

            migrationBuilder.DropTable(
                name: "productProductCategory");

            migrationBuilder.DropTable(
                name: "productProductSKU");

            migrationBuilder.DropTable(
                name: "userPin");

            migrationBuilder.DropTable(
                name: "userRole");

            migrationBuilder.DropTable(
                name: "contactType");

            migrationBuilder.DropTable(
                name: "contact");

            migrationBuilder.DropTable(
                name: "productCategory");

            migrationBuilder.DropTable(
                name: "productSKU");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
