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
                name: "bank",
                columns: table => new
                {
                    idBank = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del banco.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeBank = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Código único de la entidad bancaria. Ejemplo: BCR, BN, BAC, COOPEALIANZA."),
                    nameBank = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre completo de la entidad bancaria."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si el banco está activo y disponible para asociar cuentas bancarias.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank", x => x.idBank);
                },
                comment: "Catálogo de entidades bancarias. Representa los bancos o instituciones financieras.");

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
                    keywordRules = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "Reglas de palabras clave en formato JSON para auto-clasificar transacciones durante la importación"),
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
                name: "costCenter",
                columns: table => new
                {
                    idCostCenter = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del centro de costo.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeCostCenter = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código único del centro de costo. Ejemplo: ADM, VTA, PROD."),
                    nameCostCenter = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre descriptivo del centro de costo. Ejemplo: Administración, Ventas."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si el centro de costo está activo y disponible para su uso en asientos contables.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_costCenter", x => x.idCostCenter);
                },
                comment: "Centros de costo para clasificar los asientos contables por área, proyecto o departamento.");

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
                name: "fiscalPeriod",
                columns: table => new
                {
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del período fiscal.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    yearPeriod = table.Column<int>(type: "int", nullable: false, comment: "Año calendario del período fiscal (p. ej. 2026)."),
                    monthPeriod = table.Column<int>(type: "int", nullable: false, comment: "Número de mes del período fiscal: 1=Enero, ..., 12=Diciembre."),
                    namePeriod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nombre descriptivo del período (p. ej. 'Enero 2026')."),
                    statusPeriod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Estado del período: 'Abierto' permite movimientos, 'Cerrado' no admite nuevos registros, 'Bloqueado' está bloqueado administrativamente."),
                    startDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de inicio del período fiscal (primer día del mes)."),
                    endDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de fin del período fiscal (último día del mes).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscalPeriod", x => x.idFiscalPeriod);
                    table.CheckConstraint("CK_fiscalPeriod_statusPeriod", "statusPeriod IN ('Abierto', 'Cerrado', 'Bloqueado')");
                },
                comment: "Períodos fiscales del sistema contable. Cada período representa un mes de un año y controla si se permiten movimientos contables en ese período.");

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
                name: "bankMovementType",
                columns: table => new
                {
                    idBankMovementType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único del tipo de movimiento bancario")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeBankMovementType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Código único del tipo de movimiento (ej. DEP, RET, PAGO)"),
                    nameBankMovementType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre descriptivo del tipo de movimiento"),
                    idAccountCounterpart = table.Column<int>(type: "int", nullable: false, comment: "FK a la cuenta contable contrapartida por defecto para este tipo de movimiento"),
                    movementSign = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, comment: "Signo del movimiento: 'Cargo' (débito a la cuenta bancaria) o 'Abono' (crédito a la cuenta bancaria)"),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si el tipo de movimiento está activo")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankMovementType", x => x.idBankMovementType);
                    table.CheckConstraint("CK_bankMovementType_movementSign", "movementSign IN ('Cargo', 'Abono')");
                    table.ForeignKey(
                        name: "FK_bankMovementType_account_idAccountCounterpart",
                        column: x => x.idAccountCounterpart,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo de tipos de movimiento bancario (depósito, retiro, pago, etc.)");

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
                name: "bankAccount",
                columns: table => new
                {
                    idBankAccount = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la cuenta bancaria.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idBank = table.Column<int>(type: "int", nullable: false, comment: "FK a la entidad bancaria (banco o cooperativa) a la que pertenece esta cuenta."),
                    idAccount = table.Column<int>(type: "int", nullable: false, comment: "FK a la cuenta contable que representa esta cuenta bancaria en el mayor."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda manejada por la cuenta bancaria."),
                    codeBankAccount = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código interno único de la cuenta bancaria."),
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
                        name: "FK_bankAccount_bank_idBank",
                        column: x => x.idBank,
                        principalTable: "bank",
                        principalColumn: "idBank",
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

            migrationBuilder.CreateTable(
                name: "accountingEntry",
                columns: table => new
                {
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del asiento contable.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que pertenece el asiento contable."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda en la que fue registrado el asiento contable."),
                    numberEntry = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, comment: "Número o consecutivo del asiento contable dentro del período fiscal."),
                    dateEntry = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha contable del asiento."),
                    descriptionEntry = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false, comment: "Descripción general del asiento contable."),
                    statusEntry = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Estado del asiento contable: Borrador | Publicado | Anulado."),
                    referenceEntry = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true, comment: "Referencia opcional del asiento, como número de documento, factura o comprobante externo."),
                    exchangeRateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Tipo de cambio utilizado al momento de registrar el asiento contable."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()", comment: "Fecha y hora de creación del asiento contable.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accountingEntry", x => x.idAccountingEntry);
                    table.CheckConstraint("CK_accountingEntry_statusEntry", "statusEntry IN ('Borrador', 'Publicado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_accountingEntry_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accountingEntry_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Cabecera del asiento contable. Agrupa las líneas de débito y crédito registradas dentro de un período fiscal determinado.");

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

            migrationBuilder.CreateTable(
                name: "bankMovement",
                columns: table => new
                {
                    idBankMovement = table.Column<int>(type: "int", nullable: false, comment: "Identificador único del movimiento bancario")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idBankAccount = table.Column<int>(type: "int", nullable: false, comment: "FK a la cuenta bancaria afectada por el movimiento"),
                    idBankMovementType = table.Column<int>(type: "int", nullable: false, comment: "FK al tipo de movimiento bancario"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que pertenece el movimiento"),
                    numberMovement = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Número único del movimiento bancario (ej. MOV-2025-001)"),
                    dateMovement = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del movimiento bancario"),
                    descriptionMovement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "Descripción del movimiento bancario"),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto del movimiento en la moneda de la cuenta bancaria"),
                    statusMovement = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, defaultValue: "Borrador", comment: "Estado del movimiento: 'Borrador', 'Confirmado' o 'Anulado'"),
                    referenceMovement = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Referencia externa del movimiento (número de cheque, comprobante, etc.)"),
                    exchangeRateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Tipo de cambio vigente al momento del movimiento"),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha y hora de creación del registro en UTC")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankMovement", x => x.idBankMovement);
                    table.CheckConstraint("CK_bankMovement_statusMovement", "statusMovement IN ('Borrador', 'Confirmado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_bankMovement_bankAccount_idBankAccount",
                        column: x => x.idBankAccount,
                        principalTable: "bankAccount",
                        principalColumn: "idBankAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bankMovement_bankMovementType_idBankMovementType",
                        column: x => x.idBankMovementType,
                        principalTable: "bankMovementType",
                        principalColumn: "idBankMovementType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bankMovement_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Encabezado de movimientos bancarios (depósitos, retiros, pagos, etc.)");

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
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pendiente", comment: "Estado de la importación: Pendiente, Procesando, Completado, Fallido"),
                    totalTransactions = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Número total de transacciones en el archivo"),
                    processedTransactions = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Número de transacciones procesadas exitosamente"),
                    errorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "Mensaje de error en caso de fallo en la importación")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankStatementImport", x => x.idBankStatementImport);
                    table.CheckConstraint("CK_bankStatementImport_status", "status IN ('Pendiente', 'Procesando', 'Completado', 'Fallido')");
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
                name: "accountingEntryLine",
                columns: table => new
                {
                    idAccountingEntryLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea del asiento contable.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false, comment: "FK al asiento contable al que pertenece la línea."),
                    idAccount = table.Column<int>(type: "int", nullable: false, comment: "FK a la cuenta contable afectada por esta línea."),
                    debitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto registrado al débito. Debe ser mayor que cero solo cuando la línea es de débito."),
                    creditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto registrado al crédito. Debe ser mayor que cero solo cuando la línea es de crédito."),
                    descriptionLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "Descripción opcional y específica de la línea del asiento contable."),
                    idCostCenter = table.Column<int>(type: "int", nullable: true, comment: "FK opcional al centro de costo asociado a esta línea del asiento contable.")
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
                    table.ForeignKey(
                        name: "FK_accountingEntryLine_costCenter_idCostCenter",
                        column: x => x.idCostCenter,
                        principalTable: "costCenter",
                        principalColumn: "idCostCenter",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Líneas del asiento contable. Cada línea afecta una cuenta contable con un monto al débito o al crédito.");

            migrationBuilder.CreateTable(
                name: "bankMovementDocument",
                columns: table => new
                {
                    idBankMovementDocument = table.Column<int>(type: "int", nullable: false, comment: "Identificador único del documento de soporte")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idBankMovement = table.Column<int>(type: "int", nullable: false, comment: "FK al movimiento bancario al que pertenece el documento"),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: true, comment: "FK opcional al asiento contable vinculado a este documento"),
                    typeDocument = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Tipo de documento: 'Asiento', 'Factura', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'"),
                    numberDocument = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true, comment: "Número o referencia del documento (factura, cheque, etc.)"),
                    dateDocument = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del documento de soporte"),
                    amountDocument = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto del documento de soporte"),
                    descriptionDocument = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Descripción adicional del documento")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankMovementDocument", x => x.idBankMovementDocument);
                    table.CheckConstraint("CK_bankMovementDocument_typeDocument", "typeDocument IN ('Asiento', 'Factura', 'Recibo', 'Transferencia', 'Cheque', 'Otro')");
                    table.ForeignKey(
                        name: "FK_bankMovementDocument_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bankMovementDocument_bankMovement_idBankMovement",
                        column: x => x.idBankMovement,
                        principalTable: "bankMovement",
                        principalColumn: "idBankMovement",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Documentos de soporte vinculados a un movimiento bancario");

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
                    idBankMovementType = table.Column<int>(type: "int", nullable: true, comment: "Tipo de movimiento bancario asignado (auto o manual). Null si aún no fue clasificado"),
                    idAccountCounterpart = table.Column<int>(type: "int", nullable: true, comment: "Cuenta contable contrapartida asignada manualmente (sobrescribe la del tipo de movimiento)"),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: true, comment: "Asiento contable asociado para conciliación (opcional)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankStatementTransaction", x => x.idBankStatementTransaction);
                    table.ForeignKey(
                        name: "FK_bankStatementTransaction_account_idAccountCounterpart",
                        column: x => x.idAccountCounterpart,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bankStatementTransaction_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bankStatementTransaction_bankMovementType_idBankMovementType",
                        column: x => x.idBankMovementType,
                        principalTable: "bankMovementType",
                        principalColumn: "idBankMovementType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bankStatementTransaction_bankStatementImport_idBankStatementImport",
                        column: x => x.idBankStatementImport,
                        principalTable: "bankStatementImport",
                        principalColumn: "idBankStatementImport",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Transacciones individuales importadas de extractos bancarios");

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
                table: "bank",
                columns: new[] { "idBank", "codeBank", "isActive", "nameBank" },
                values: new object[,]
                {
                    { 1, "BCR", true, "Banco de Costa Rica" },
                    { 2, "BAC", true, "BAC Credomatic" },
                    { 3, "BN", true, "Banco Nacional de Costa Rica" },
                    { 4, "COOPEALIANZA", true, "Coopealianza" },
                    { 5, "DAVIVIENDA", true, "Davivienda" },
                    { 6, "BPOPULAR", true, "Banco Popular y de Desarrollo Comunal" }
                });

            migrationBuilder.InsertData(
                table: "bankStatementTemplate",
                columns: new[] { "idBankStatementTemplate", "bankName", "codeTemplate", "columnMappings", "dateFormat", "isActive", "keywordRules", "nameTemplate", "notes", "timeFormat" },
                values: new object[] { 1, "Banco de Costa Rica", "BCR-HTML-XLS-V1", "{\"accountingDate\":0,\"transactionDate\":1,\"transactionTime\":2,\"documentNumber\":3,\"description\":4,\"debitAmount\":5,\"creditAmount\":6,\"balance\":7,\"skipHeaderRows\":1}", "dd/MM/yyyy", true, "[\r\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\r\n                                                                        \"idBankMovementType\":1,\"matchMode\":\"Any\"},\r\n  {\"keywords\":[\"DEP EFECTIVO\",\"DEPOSITO EFECTIVO\",\"DEPOSITO EN CAJA\"],\r\n                                                                        \"idBankMovementType\":2,\"matchMode\":\"Any\"},\r\n  {\"keywords\":[\"INTERNET DTR SINPE\",\"DTR SINPE\",\"SINPE CR\",\"TRANSF CREDIT\",\"CREDITO SINPE\",\"SINPE MOVIL CR\",\"ABONO SINPE\",\"RECIBO SINPE\"],\r\n                                                                        \"idBankMovementType\":3,\"matchMode\":\"Any\"},\r\n  {\"keywords\":[\"COMPRAS EN COMERCIOS\",\"COMPRA EN COMERCIO\",\"COMPRAS COMERC\",\"COMPRA COMERC\"],\r\n                                                                        \"idBankMovementType\":4,\"matchMode\":\"Any\"},\r\n  {\"keywords\":[\"RETIRO ATM\",\"RETIRO CAJERO\",\"RETIRO EFECTIVO\",\"CAJERO AUTOMATICO\"],\r\n                                                                        \"idBankMovementType\":5,\"matchMode\":\"Any\"},\r\n  {\"keywords\":[\"PAGO TC\",\"PAGO TARJETA\",\"TRJ CRED\",\"PAGO TARJETA CREDITO\",\"PAGO TRJ\",\"PAGO TARJETAS\"],\r\n                                                                        \"idBankMovementType\":6,\"matchMode\":\"Any\"},\r\n  {\"keywords\":[\"PAGO PREST\",\"CUOTA PREST\",\"PAGO PRESTAMO\",\"CUOTA PRESTAMO\"],\r\n                                                                        \"idBankMovementType\":7,\"matchMode\":\"Any\"},\r\n  {\"keywords\":[\"SINPE MOVIL OTRA ENT\",\"OTRA ENT\",\"TRANSF DEB\",\"SINPE DEB\",\"DEB SINPE\",\"SINPE MOVIL DEB\",\"DEBITO SINPE\",\"TRANSFERENCIA SINPE DEB\",\"CARGO SINPE\"],\r\n                                                                        \"idBankMovementType\":8,\"matchMode\":\"Any\"}\r\n]", "BCR – Movimientos de Cuenta (HTML-XLS)", "Archivo exportado como .xls desde el portal BCR. El contenido real es HTML con una tabla id='t1'. Aplica para cuentas de ahorros y cuentas corrientes en colones y dólares.", "HH:mm:ss" });

            migrationBuilder.InsertData(
                table: "contactType",
                columns: new[] { "idContactType", "codeContactType", "name" },
                values: new object[,]
                {
                    { 1, "CLI", "Cliente" },
                    { 2, "PRO", "Proveedor" }
                });

            migrationBuilder.InsertData(
                table: "costCenter",
                columns: new[] { "idCostCenter", "codeCostCenter", "isActive", "nameCostCenter" },
                values: new object[,]
                {
                    { 1, "FAM-KYE", true, "Familia Baltodano Soto (K & E)" },
                    { 2, "FAM-PAPA", true, "Familia Baltodano Cubillo (Papás)" },
                    { 3, "OTROS", true, "Otros" }
                });

            migrationBuilder.InsertData(
                table: "currency",
                columns: new[] { "idCurrency", "codeCurrency", "nameCurrency", "symbolCurrency" },
                values: new object[,]
                {
                    { 1, "CRC", "Colón costarricense", "₡" },
                    { 2, "USD", "Dólar estadounidense", "$" }
                });

            migrationBuilder.InsertData(
                table: "fiscalPeriod",
                columns: new[] { "idFiscalPeriod", "endDate", "monthPeriod", "namePeriod", "startDate", "statusPeriod", "yearPeriod" },
                values: new object[,]
                {
                    { 1, new DateOnly(2026, 1, 31), 1, "Enero 2026", new DateOnly(2026, 1, 1), "Abierto", 2026 },
                    { 2, new DateOnly(2026, 2, 28), 2, "Febrero 2026", new DateOnly(2026, 2, 1), "Abierto", 2026 },
                    { 3, new DateOnly(2026, 3, 31), 3, "Marzo 2026", new DateOnly(2026, 3, 1), "Abierto", 2026 },
                    { 4, new DateOnly(2026, 4, 30), 4, "Abril 2026", new DateOnly(2026, 4, 1), "Abierto", 2026 },
                    { 5, new DateOnly(2026, 5, 31), 5, "Mayo 2026", new DateOnly(2026, 5, 1), "Abierto", 2026 },
                    { 6, new DateOnly(2026, 6, 30), 6, "Junio 2026", new DateOnly(2026, 6, 1), "Abierto", 2026 },
                    { 7, new DateOnly(2026, 7, 31), 7, "Julio 2026", new DateOnly(2026, 7, 1), "Abierto", 2026 },
                    { 8, new DateOnly(2026, 8, 31), 8, "Agosto 2026", new DateOnly(2026, 8, 1), "Abierto", 2026 },
                    { 9, new DateOnly(2026, 9, 30), 9, "Septiembre 2026", new DateOnly(2026, 9, 1), "Abierto", 2026 },
                    { 10, new DateOnly(2026, 10, 31), 10, "Octubre 2026", new DateOnly(2026, 10, 1), "Abierto", 2026 },
                    { 11, new DateOnly(2026, 11, 30), 11, "Noviembre 2026", new DateOnly(2026, 11, 1), "Abierto", 2026 },
                    { 12, new DateOnly(2026, 12, 31), 12, "Diciembre 2026", new DateOnly(2026, 12, 1), "Abierto", 2026 }
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
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[,]
                {
                    { 7, false, "1.1", 1, true, 2, "Activo Corriente", "Activo" },
                    { 8, false, "1.2", 1, true, 2, "Activo No Corriente", "Activo" },
                    { 9, false, "2.1", 2, true, 2, "Pasivo Corriente", "Pasivo" },
                    { 10, false, "2.2", 2, true, 2, "Pasivo No Corriente", "Pasivo" },
                    { 11, true, "3.1", 3, true, 2, "Utilidad Acumulada", "Capital" },
                    { 12, true, "3.2", 3, true, 2, "Utilidad del Período", "Capital" },
                    { 13, false, "4.1", 4, true, 2, "Salario / Sueldos", "Ingreso" },
                    { 14, false, "4.2", 4, true, 2, "Servicios Profesionales", "Ingreso" },
                    { 15, false, "4.3", 4, true, 2, "Otros Ingresos", "Ingreso" },
                    { 45, false, "5.1", 5, true, 2, "Cargas Sociales e Impuestos", "Gasto" },
                    { 59, false, "5.2", 5, true, 2, "Vivienda", "Gasto" },
                    { 90, false, "5.3", 5, true, 2, "Alimentación", "Gasto" },
                    { 91, false, "5.4", 5, true, 2, "Transporte", "Gasto" },
                    { 92, false, "5.5", 5, true, 2, "Finanzas", "Gasto" },
                    { 93, false, "5.6", 5, true, 2, "Educación", "Gasto" },
                    { 94, false, "5.7", 5, true, 2, "Comunicaciones", "Gasto" },
                    { 95, false, "5.8", 5, true, 2, "Suscripciones", "Gasto" },
                    { 96, false, "5.12", 5, true, 2, "Otros", "Gasto" },
                    { 97, false, "5.9", 5, true, 2, "Servicios del Hogar", "Gasto" },
                    { 99, false, "5.10", 5, true, 2, "Personal y Hogar", "Gasto" },
                    { 100, false, "5.11", 5, true, 2, "Obligaciones", "Gasto" },
                    { 101, false, "4.4", 4, true, 2, "Ingresos Financieros", "Ingreso" },
                    { 103, false, "5.13", 5, true, 2, "Gastos Financieros", "Gasto" }
                });

            migrationBuilder.InsertData(
                table: "userRole",
                columns: new[] { "idUserRole", "createAt", "idRole", "idUser" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 1 },
                    { 2, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 1 },
                    { 3, new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 1 }
                });

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[,]
                {
                    { 24, false, "1.1.01", 7, true, 3, "Banco de Costa Rica (BCR)", "Activo" },
                    { 26, false, "1.1.02", 7, true, 3, "BAC Credomatic", "Activo" },
                    { 28, false, "2.1.01", 9, true, 3, "BAC Credomatic - Tarjetas", "Pasivo" },
                    { 33, false, "1.1.03", 7, true, 3, "Banco Nacional de Costa Rica (BN)", "Activo" },
                    { 36, false, "1.2.01", 8, true, 3, "Coopealianza", "Activo" },
                    { 38, false, "1.1.04", 7, true, 3, "Coopealianza", "Activo" },
                    { 40, false, "2.1.02", 9, true, 3, "Coopealianza - Préstamos", "Pasivo" },
                    { 41, false, "2.2.01", 10, true, 3, "Coopealianza - Préstamos", "Pasivo" },
                    { 43, false, "4.1.01", 13, true, 3, "IT Quest Solutions (ITQS)", "Ingreso" },
                    { 46, true, "5.1.01", 45, true, 3, "Impuesto de Renta", "Gasto" },
                    { 47, true, "5.1.02", 45, true, 3, "CCSS - SEM Trabajador", "Gasto" },
                    { 48, false, "1.2.02", 8, true, 3, "CCSS - Fondo de Pensión (IVM)", "Activo" },
                    { 50, false, "1.2.03", 8, true, 3, "Banco Popular - LPT (Fondo Capitalización Laboral)", "Activo" },
                    { 52, false, "2.1.03", 9, true, 3, "Adelantos Salariales por Liquidar", "Pasivo" },
                    { 54, false, "1.1.05", 7, true, 3, "Davivienda", "Activo" },
                    { 60, true, "5.2.01", 59, true, 3, "Alquiler", "Gasto" },
                    { 61, true, "5.3.01", 90, true, 3, "Alimentación", "Gasto" },
                    { 62, true, "5.4.01", 91, true, 3, "Gasolina", "Gasto" },
                    { 63, true, "5.5.01", 92, true, 3, "Tarjeta BN", "Gasto" },
                    { 64, true, "5.5.02", 92, true, 3, "Tarjeta BAC Tasa Cero", "Gasto" },
                    { 65, true, "5.5.03", 92, true, 3, "Tarjeta BAC Préstamo 2M", "Gasto" },
                    { 66, true, "5.5.04", 92, true, 3, "Coopealianza", "Gasto" },
                    { 67, true, "5.6.01", 93, true, 3, "Clases de Inglés", "Gasto" },
                    { 68, true, "5.7.01", 94, true, 3, "Teléfono Celular", "Gasto" },
                    { 69, true, "5.8.01", 95, true, 3, "Netflix", "Gasto" },
                    { 70, true, "5.8.02", 95, true, 3, "App Anime", "Gasto" },
                    { 71, true, "5.8.03", 95, true, 3, "Apple Music", "Gasto" },
                    { 72, true, "5.8.04", 95, true, 3, "Apple iCloud", "Gasto" },
                    { 73, true, "5.8.05", 95, true, 3, "ChatGPT", "Gasto" },
                    { 74, true, "5.8.06", 95, true, 3, "Copilot", "Gasto" },
                    { 75, true, "5.12.01", 96, true, 3, "Gastos en Pareja", "Gasto" },
                    { 77, true, "5.10.01", 99, true, 3, "Aporte Familiar", "Gasto" },
                    { 79, true, "5.7.02", 94, true, 3, "Internet", "Gasto" },
                    { 80, true, "5.9.01", 97, true, 3, "AyA", "Gasto" },
                    { 81, true, "5.9.02", 97, true, 3, "CNFL", "Gasto" },
                    { 82, true, "5.9.03", 97, true, 3, "Teléfono Casa", "Gasto" },
                    { 83, true, "5.4.02", 91, true, 3, "Transporte Actividades", "Gasto" },
                    { 84, true, "5.4.03", 91, true, 3, "Transporte Citas", "Gasto" },
                    { 85, true, "5.10.02", 99, true, 3, "Ayuda en Casa", "Gasto" },
                    { 86, true, "5.11.01", 100, true, 3, "Municipalidad", "Gasto" },
                    { 87, true, "5.11.02", 100, true, 3, "Campo Santo", "Gasto" },
                    { 88, true, "5.11.03", 100, true, 3, "Campo Santo Mantenimiento", "Gasto" },
                    { 102, true, "4.4.01", 101, true, 3, "Diferencial Cambiario Favorable", "Ingreso" },
                    { 104, true, "5.13.01", 103, true, 3, "Diferencial Cambiario Desfavorable", "Gasto" }
                });

            migrationBuilder.InsertData(
                table: "bankMovementType",
                columns: new[] { "idBankMovementType", "codeBankMovementType", "idAccountCounterpart", "isActive", "movementSign", "nameBankMovementType" },
                values: new object[,]
                {
                    { 2, "DEP", 15, true, "Abono", "Depósito en Efectivo" },
                    { 3, "TRANSF-REC", 15, true, "Abono", "Transferencia Recibida" },
                    { 4, "GASTO", 96, true, "Cargo", "Gasto General" }
                });

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[,]
                {
                    { 25, true, "1.1.01.01", 24, true, 4, "BCR - Cta. 07015202001294229652 - Soto Arce Karen Tatiana", "Activo" },
                    { 27, true, "1.1.02.01", 26, true, 4, "BAC - Cta. CR73010200009497305680 - Baltodano Cubillo Ezequiel", "Activo" },
                    { 29, true, "2.1.01.01", 28, true, 4, "BAC - AMEX ****-8052 - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 30, true, "2.1.01.02", 28, true, 4, "BAC - MCARD ****-6515 - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 31, true, "2.1.01.03", 28, true, 4, "BAC - MCARD ****-8608 - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 32, true, "2.1.01.04", 28, true, 4, "BAC - VISA ****-1593 - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 34, true, "1.1.03.01", 33, true, 4, "BN - Cta. CR86015100020019688637 (₡) - Baltodano Cubillo Ezequiel", "Activo" },
                    { 35, true, "1.1.03.02", 33, true, 4, "BN - Cta. CR06015107220020012339 ($) - Baltodano Cubillo Ezequiel", "Activo" },
                    { 37, true, "1.2.01.01", 36, true, 4, "Coopealianza - Aporte al Patrimonio CR02081300010008440263 (₡) - Baltodano Cubillo Ezequiel", "Activo" },
                    { 39, true, "1.1.04.01", 38, true, 4, "Coopealianza - Cta. CR54081300210008440287 (₡) - Baltodano Cubillo Ezequiel", "Activo" },
                    { 42, true, "2.2.01.01", 41, true, 4, "Coopealianza - Préstamo CR05081302810003488995 (₡) - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 44, true, "4.1.01.01", 43, true, 4, "ITQS - Salario Ordinario Mensual CLS - Baltodano Cubillo Ezequiel", "Ingreso" },
                    { 49, true, "1.2.02.01", 48, true, 4, "CCSS - IVM Trabajador - Baltodano Cubillo Ezequiel", "Activo" },
                    { 51, true, "1.2.03.01", 50, true, 4, "Banco Popular - LPT - Baltodano Cubillo Ezequiel", "Activo" },
                    { 53, true, "2.1.03.01", 52, true, 4, "Adelanto Salarial ITQS - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 55, true, "1.1.05.01", 54, true, 4, "Davivienda - AHO CR98010401446613244113 (₡) - Baltodano Cubillo Ezequiel [Nómina ITQS]", "Activo" }
                });

            migrationBuilder.InsertData(
                table: "bankMovementType",
                columns: new[] { "idBankMovementType", "codeBankMovementType", "idAccountCounterpart", "isActive", "movementSign", "nameBankMovementType" },
                values: new object[,]
                {
                    { 5, "RET", 75, true, "Cargo", "Retiro en Efectivo" },
                    { 6, "PAGO-TC", 28, true, "Cargo", "Pago Tarjeta de Crédito" }
                });

            migrationBuilder.InsertData(
                table: "bankAccount",
                columns: new[] { "idBankAccount", "accountHolder", "accountNumber", "codeBankAccount", "idAccount", "idBank", "idCurrency", "isActive" },
                values: new object[,]
                {
                    { 1, "Soto Arce Karen Tatiana", "07015202001294229652", "BCR-AHO-001", 25, 1, 1, true },
                    { 2, "Baltodano Cubillo Ezequiel", "CR73010200009497305680", "BAC-AHO-001", 27, 2, 1, true },
                    { 3, "Baltodano Cubillo Ezequiel", "****-8052", "BAC-CC-AMEX-8052", 29, 2, 1, true },
                    { 4, "Baltodano Cubillo Ezequiel", "****-6515", "BAC-CC-MC-6515", 30, 2, 1, true },
                    { 5, "Baltodano Cubillo Ezequiel", "****-8608", "BAC-CC-MC-8608", 31, 2, 1, true },
                    { 6, "Baltodano Cubillo Ezequiel", "****-1593", "BAC-CC-VISA-1593", 32, 2, 1, true },
                    { 7, "Baltodano Cubillo Ezequiel", "CR86015100020019688637", "BN-AHO-CRC-001", 34, 3, 1, true },
                    { 8, "Baltodano Cubillo Ezequiel", "CR06015107220020012339", "BN-AHO-USD-001", 35, 3, 2, true },
                    { 9, "Baltodano Cubillo Ezequiel", "CR54081300210008440287", "COOPEAL-AHO-001", 39, 4, 1, true },
                    { 10, "Baltodano Cubillo Ezequiel", "CR02081300010008440263", "COOPEAL-PAT-001", 37, 4, 1, true },
                    { 11, "Baltodano Cubillo Ezequiel", "CR98010401446613244113", "DAVIV-AHO-001", 55, 5, 1, true }
                });

            migrationBuilder.InsertData(
                table: "bankMovementType",
                columns: new[] { "idBankMovementType", "codeBankMovementType", "idAccountCounterpart", "isActive", "movementSign", "nameBankMovementType" },
                values: new object[,]
                {
                    { 1, "SAL", 44, true, "Abono", "Depósito de Salario" },
                    { 7, "PAGO-PREST", 42, true, "Cargo", "Pago de Préstamo" },
                    { 8, "TRANSF-ENV", 34, true, "Cargo", "Transferencia Enviada" }
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
                name: "IX_accountingEntry_idCurrency",
                table: "accountingEntry",
                column: "idCurrency");

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

            migrationBuilder.CreateIndex(
                name: "IX_accountingEntryLine_idCostCenter",
                table: "accountingEntryLine",
                column: "idCostCenter",
                filter: "[idCostCenter] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_bank_codeBank",
                table: "bank",
                column: "codeBank",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bankAccount_idAccount",
                table: "bankAccount",
                column: "idAccount");

            migrationBuilder.CreateIndex(
                name: "IX_bankAccount_idBank",
                table: "bankAccount",
                column: "idBank");

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
                name: "IX_bankMovement_idBankAccount",
                table: "bankMovement",
                column: "idBankAccount");

            migrationBuilder.CreateIndex(
                name: "IX_bankMovement_idBankMovementType",
                table: "bankMovement",
                column: "idBankMovementType");

            migrationBuilder.CreateIndex(
                name: "IX_bankMovement_idFiscalPeriod",
                table: "bankMovement",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "UQ_bankMovement_numberMovement",
                table: "bankMovement",
                column: "numberMovement",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bankMovementDocument_idAccountingEntry",
                table: "bankMovementDocument",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "IX_bankMovementDocument_idBankMovement",
                table: "bankMovementDocument",
                column: "idBankMovement");

            migrationBuilder.CreateIndex(
                name: "IX_bankMovementType_idAccountCounterpart",
                table: "bankMovementType",
                column: "idAccountCounterpart");

            migrationBuilder.CreateIndex(
                name: "UQ_bankMovementType_codeBankMovementType",
                table: "bankMovementType",
                column: "codeBankMovementType",
                unique: true);

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
                name: "IX_bankStatementTransaction_idAccountCounterpart",
                table: "bankStatementTransaction",
                column: "idAccountCounterpart");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementTransaction_idAccountingEntry",
                table: "bankStatementTransaction",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementTransaction_idBankMovementType",
                table: "bankStatementTransaction",
                column: "idBankMovementType");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementTransaction_idBankStatementImport",
                table: "bankStatementTransaction",
                column: "idBankStatementImport");

            migrationBuilder.CreateIndex(
                name: "IX_bankStatementTransaction_isReconciled",
                table: "bankStatementTransaction",
                column: "isReconciled");

            migrationBuilder.CreateIndex(
                name: "IX_budget_idFiscalPeriod",
                table: "budget",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "UQ_budget_idAccount_idFiscalPeriod",
                table: "budget",
                columns: new[] { "idAccount", "idFiscalPeriod" },
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
                name: "UQ_costCenter_codeCostCenter",
                table: "costCenter",
                column: "codeCostCenter",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "UQ_fiscalPeriod_yearPeriod_monthPeriod",
                table: "fiscalPeriod",
                columns: new[] { "yearPeriod", "monthPeriod" },
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
                name: "accountingEntryLine");

            migrationBuilder.DropTable(
                name: "bankMovementDocument");

            migrationBuilder.DropTable(
                name: "bankStatementTransaction");

            migrationBuilder.DropTable(
                name: "budget");

            migrationBuilder.DropTable(
                name: "contactContactType");

            migrationBuilder.DropTable(
                name: "exchangeRate");

            migrationBuilder.DropTable(
                name: "productProductCategory");

            migrationBuilder.DropTable(
                name: "productProductSKU");

            migrationBuilder.DropTable(
                name: "userPin");

            migrationBuilder.DropTable(
                name: "userRole");

            migrationBuilder.DropTable(
                name: "costCenter");

            migrationBuilder.DropTable(
                name: "bankMovement");

            migrationBuilder.DropTable(
                name: "accountingEntry");

            migrationBuilder.DropTable(
                name: "bankStatementImport");

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
                name: "bankMovementType");

            migrationBuilder.DropTable(
                name: "fiscalPeriod");

            migrationBuilder.DropTable(
                name: "bankAccount");

            migrationBuilder.DropTable(
                name: "bankStatementTemplate");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "bank");

            migrationBuilder.DropTable(
                name: "currency");
        }
    }
}
