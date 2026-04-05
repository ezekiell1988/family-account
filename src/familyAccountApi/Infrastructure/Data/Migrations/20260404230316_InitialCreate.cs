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
                name: "company",
                columns: table => new
                {
                    idCompany = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la empresa.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeCompany = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código único de la empresa."),
                    nameCompany = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre completo de la empresa.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.idCompany);
                },
                comment: "Empresas registradas en el sistema.");

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
                name: "productType",
                columns: table => new
                {
                    idProductType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de producto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nameProductType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, comment: "Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa."),
                    descriptionProductType = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "Descripción del tipo de producto y sus reglas de negocio.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productType", x => x.idProductType);
                },
                comment: "Tipo de producto según la fase productiva: Materia Prima, Producto en Proceso, Producto Terminado o Reventa. Catálogo de sistema, sin CRUD expuesto al usuario.");

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
                name: "unitOfMeasure",
                columns: table => new
                {
                    idUnit = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la unidad de medida.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeUnit = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, comment: "Código corto de la unidad: ML, GR, KG, LTR, BOT160, LATA400, UNI, etc."),
                    nameUnit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "Nombre legible de la unidad: Mililitro, Gramo, Botella 160ml, etc."),
                    typeUnit = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Clasificación dimensional: Volumen | Masa | Unidad | Longitud.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unitOfMeasure", x => x.idUnit);
                    table.CheckConstraint("CK_unitOfMeasure_typeUnit", "typeUnit IN ('Volumen', 'Masa', 'Unidad', 'Longitud')");
                },
                comment: "Catálogo global de unidades de medida utilizadas en productos, recetas e inventario.");

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
                name: "inventoryAdjustmentType",
                columns: table => new
                {
                    idInventoryAdjustmentType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de ajuste.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeInventoryAdjustmentType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Código único del tipo: 'CONTEO', 'PRODUCCION', 'AJUSTE_COSTO'."),
                    nameInventoryAdjustmentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre descriptivo del tipo (ej. 'Conteo Físico', 'Producción', 'Ajuste de Costo')."),
                    idAccountInventoryDefault = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta de activo de inventario. DR en entradas (delta+), CR en salidas (delta-). Si es null, no se genera asiento contable al confirmar."),
                    idAccountCounterpartEntry = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta contrapartida para entradas (delta > 0) o ajuste de costo al alza. Actúa como CR del asiento. Ej: 'Ajuste Favorable de Inventario'."),
                    idAccountCounterpartExit = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta contrapartida para salidas (delta < 0) o ajuste de costo a la baja. Actúa como DR del asiento. Ej: 'Gasto por Merma', 'Costo de Producción'."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si el tipo está activo y disponible para nuevos ajustes.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryAdjustmentType", x => x.idInventoryAdjustmentType);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustmentType_account_idAccountCounterpartEntry",
                        column: x => x.idAccountCounterpartEntry,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustmentType_account_idAccountCounterpartExit",
                        column: x => x.idAccountCounterpartExit,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustmentType_account_idAccountInventoryDefault",
                        column: x => x.idAccountInventoryDefault,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo de tipos de ajuste de inventario. Define las cuentas contables usadas para generar el asiento al confirmar un ajuste: cuenta de inventario (activo), cuenta contrapartida de entrada y cuenta contrapartida de salida.");

            migrationBuilder.CreateTable(
                name: "companyDomain",
                columns: table => new
                {
                    idDomain = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    domainUrl = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    idCompany = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companyDomain", x => x.idDomain);
                    table.ForeignKey(
                        name: "FK_companyDomain_company_idCompany",
                        column: x => x.idCompany,
                        principalTable: "company",
                        principalColumn: "idCompany",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "companyWhatsapp",
                columns: table => new
                {
                    idCompanyWhatsapp = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idCompany = table.Column<int>(type: "int", nullable: false),
                    phoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    phoneNumberId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    wabaId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    accessToken = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    webhookVerifyToken = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    apiVersion = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companyWhatsapp", x => x.idCompanyWhatsapp);
                    table.ForeignKey(
                        name: "FK_companyWhatsapp_company_idCompany",
                        column: x => x.idCompany,
                        principalTable: "company",
                        principalColumn: "idCompany",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()", comment: "Fecha y hora de creación del asiento contable."),
                    originModule = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true, comment: "Módulo que generó este asiento automáticamente: null (manual) | 'BankMovement' | 'PurchaseInvoice'. Los asientos con origen definido son solo lectura desde la vista general de asientos."),
                    idOriginRecord = table.Column<int>(type: "int", nullable: true, comment: "ID del registro de origen (IdBankMovement o IdPurchaseInvoice). Sin FK física — referencia polimórfica controlada en la capa de servicio.")
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
                name: "product",
                columns: table => new
                {
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del producto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeProduct = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Código interno único del producto definido por la empresa."),
                    nameProduct = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre interno del producto usado en el sistema."),
                    idProductType = table.Column<int>(type: "int", nullable: false, comment: "FK al tipo de producto: Materia Prima, Prod. en Proceso, Prod. Terminado o Reventa."),
                    idUnit = table.Column<int>(type: "int", nullable: false, comment: "FK a la unidad de medida base del producto. Es la unidad en la que se lleva el inventario y se expresan las recetas."),
                    idProductParent = table.Column<int>(type: "int", nullable: true, comment: "FK auto-referencial al producto padre. Agrupa variantes bajo un mismo producto (máximo un nivel). NULL si es raíz."),
                    averageCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 0m, comment: "Costo promedio ponderado en unidad base. Se recalcula automáticamente al confirmar compras y ajustes con stock positivo."),
                    hasOptions = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Indica que el producto tiene grupos de opciones configurables por el cliente (ej: tamaño, masa, sabor)."),
                    isCombo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Indica que el producto es un combo compuesto de slots con productos elegibles.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.idProduct);
                    table.ForeignKey(
                        name: "FK_product_productType_idProductType",
                        column: x => x.idProductType,
                        principalTable: "productType",
                        principalColumn: "idProductType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_product_idProductParent",
                        column: x => x.idProductParent,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_unitOfMeasure_idUnit",
                        column: x => x.idUnit,
                        principalTable: "unitOfMeasure",
                        principalColumn: "idUnit",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo interno de productos. Cada producto tiene un tipo (Materia Prima, Producto en Proceso, Producto Terminado, Reventa), una unidad base de inventario y opcionalmente un producto padre para agrupar variantes.");

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
                name: "purchaseInvoiceType",
                columns: table => new
                {
                    idPurchaseInvoiceType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de factura de compra.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codePurchaseInvoiceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Código único del tipo: 'EFECTIVO', 'DEBITO', 'TC'."),
                    namePurchaseInvoiceType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre descriptivo del tipo de factura (ej. 'Tarjeta de Crédito')."),
                    counterpartFromBankMovement = table.Column<bool>(type: "bit", nullable: false, comment: "true = la cuenta CR del asiento se toma del BankAccount vinculado al BankMovement (DEBITO, TC). false = la cuenta CR es fija (EFECTIVO: Caja CRC o Caja USD según moneda)."),
                    idAccountCounterpartCRC = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta Caja CRC. Solo aplica cuando CounterpartFromBankMovement = false (tipo EFECTIVO). La selección entre CRC o USD se hace automáticamente según la moneda de la factura."),
                    idAccountCounterpartUSD = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta Caja USD. Solo aplica cuando CounterpartFromBankMovement = false (tipo EFECTIVO)."),
                    idBankMovementType = table.Column<int>(type: "int", nullable: true, comment: "FK al tipo de movimiento bancario usado para auto-crear el BankMovement al confirmar. Solo aplica cuando CounterpartFromBankMovement = true (DEBITO, TC)."),
                    idDefaultExpenseAccount = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta contable de gasto usada como fallback cuando el SKU de la línea no tiene ProductAccount configurado. Permite confirmar facturas aunque los productos no tengan distribución contable."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si el tipo de factura está activo y disponible para registrar nuevas facturas.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseInvoiceType", x => x.idPurchaseInvoiceType);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceType_account_idAccountCounterpartCRC",
                        column: x => x.idAccountCounterpartCRC,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceType_account_idAccountCounterpartUSD",
                        column: x => x.idAccountCounterpartUSD,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceType_account_idDefaultExpenseAccount",
                        column: x => x.idDefaultExpenseAccount,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceType_bankMovementType_idBankMovementType",
                        column: x => x.idBankMovementType,
                        principalTable: "bankMovementType",
                        principalColumn: "idBankMovementType",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo de tipos de factura de compra. Define si la contrapartida contable (CR) proviene del BankMovement vinculado o de una cuenta Caja fija por moneda.");

            migrationBuilder.CreateTable(
                name: "inventoryAdjustment",
                columns: table => new
                {
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del ajuste.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que corresponde este ajuste."),
                    idInventoryAdjustmentType = table.Column<int>(type: "int", nullable: false, comment: "FK al tipo de ajuste. Determina las cuentas contables del asiento generado al confirmar."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda del ajuste. Se usa en el asiento contable generado."),
                    exchangeRateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Tipo de cambio vigente al momento del ajuste. 1.0 para moneda local."),
                    numberAdjustment = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Consecutivo interno generado al confirmar. Formato: AJ-YYYYMMDD-NNN."),
                    dateAdjustment = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del evento: conteo físico, corrida de producción o ajuste de costo."),
                    descriptionAdjustment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Motivo o descripción del ajuste (ej: Conteo físico mensual, Corrida lote 26032002, NC proveedor)."),
                    statusAdjustment = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Estado del ajuste: Borrador | Confirmado | Anulado."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryAdjustment", x => x.idInventoryAdjustment);
                    table.CheckConstraint("CK_inventoryAdjustment_statusAdjustment", "statusAdjustment IN ('Borrador', 'Confirmado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_inventoryAdjustment_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustment_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustment_inventoryAdjustmentType_idInventoryAdjustmentType",
                        column: x => x.idInventoryAdjustmentType,
                        principalTable: "inventoryAdjustmentType",
                        principalColumn: "idInventoryAdjustmentType",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Documento de ajuste de inventario. El tipo (idInventoryAdjustmentType) define las cuentas contables para generar el asiento al confirmar. Estados: Borrador → Confirmado → Anulado.");

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
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha y hora de creación del registro en UTC"),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: true, comment: "FK opcional al asiento contable generado al confirmar el movimiento bancario. Relación 1:1 con AccountingEntry.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankMovement", x => x.idBankMovement);
                    table.CheckConstraint("CK_bankMovement_statusMovement", "statusMovement IN ('Borrador', 'Confirmado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_bankMovement_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Restrict);
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
                name: "productAccount",
                columns: table => new
                {
                    idProductAccount = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la distribución contable del producto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto que se está distribuyendo contablemente."),
                    idAccount = table.Column<int>(type: "int", nullable: false, comment: "FK a la cuenta contable de gasto (DR del asiento de factura)."),
                    idCostCenter = table.Column<int>(type: "int", nullable: true, comment: "FK opcional al centro de costo. Nullable cuando el producto no requiere distribución por centro de costo."),
                    percentageAccount = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "Porcentaje del total de la línea asignado a esta cuenta/centro de costo. La suma por IdProduct debe ser 100.00."),
                    productIdProduct = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productAccount", x => x.idProductAccount);
                    table.ForeignKey(
                        name: "FK_productAccount_account_idAccount",
                        column: x => x.idAccount,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productAccount_costCenter_idCostCenter",
                        column: x => x.idCostCenter,
                        principalTable: "costCenter",
                        principalColumn: "idCostCenter",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_productAccount_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productAccount_product_productIdProduct",
                        column: x => x.productIdProduct,
                        principalTable: "product",
                        principalColumn: "idProduct");
                },
                comment: "Distribución contable por producto: define la cuenta de gasto y el centro de costo para cada porcentaje del total de la línea de factura. La suma de PercentageAccount por IdProduct debe ser exactamente 100.");

            migrationBuilder.CreateTable(
                name: "productComboSlot",
                columns: table => new
                {
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del slot del combo.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductCombo = table.Column<int>(type: "int", nullable: false, comment: "FK al producto combo padre (IsCombo=true)."),
                    nameSlot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre visible del slot (ej: Pizza #1, Bebida)."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este slot dentro del combo."),
                    isRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Si el cliente debe llenar este slot obligatoriamente."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación del slot al cliente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productComboSlot", x => x.idProductComboSlot);
                    table.ForeignKey(
                        name: "FK_productComboSlot_product_idProductCombo",
                        column: x => x.idProductCombo,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Slots de un combo (ej: Pizza #1, Pizza #2, Bebida). Un producto con IsCombo=true tiene N slots.");

            migrationBuilder.CreateTable(
                name: "productOptionGroup",
                columns: table => new
                {
                    idProductOptionGroup = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del grupo de opciones.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto configurable al que pertenece este grupo."),
                    nameGroup = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre visible del grupo (ej: Elige tu tamaño)."),
                    isRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Si el cliente debe elegir obligatoriamente en este grupo."),
                    minSelections = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "Mínimo de items a elegir. 0 para grupos opcionales."),
                    maxSelections = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "Máximo de items a elegir. 1 para exclusivo, N para múltiple."),
                    allowSplit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Cuando true, en modo mitad/mitad el cliente asigna cada selección a una mitad (half1|half2|whole). Aplica a grupos de sabor y adicionales."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación del grupo al cliente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productOptionGroup", x => x.idProductOptionGroup);
                    table.ForeignKey(
                        name: "FK_productOptionGroup_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Grupos de opciones configurables de un producto (ej: Tamaño, Masa, Sabor). Un producto con HasOptions=true puede tener N grupos.");

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
                name: "productRecipe",
                columns: table => new
                {
                    idProductRecipe = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la receta.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductOutput = table.Column<int>(type: "int", nullable: false, comment: "FK al producto que produce esta receta. No puede ser Materia Prima ni Reventa."),
                    nameRecipe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre descriptivo de la receta (ej: Cahuita Salsa Caribeña 160ml v2)."),
                    quantityOutput = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad producida por corrida, expresada en la unidad base del producto output."),
                    descriptionRecipe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Instrucciones generales u observaciones del proceso productivo."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Solo recetas activas se usan en producción."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productRecipe", x => x.idProductRecipe);
                    table.ForeignKey(
                        name: "FK_productRecipe_product_idProductOutput",
                        column: x => x.idProductOutput,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Recetas (BOM - Bill of Materials) para la producción de un producto. Define qué insumos se consumen y en qué cantidades para producir una corrida del output. Solo puede ser output un producto de tipo Producto en Proceso o Producto Terminado.");

            migrationBuilder.CreateTable(
                name: "productUnit",
                columns: table => new
                {
                    idProductUnit = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la presentación del producto.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto al que pertenece esta presentación."),
                    idUnit = table.Column<int>(type: "int", nullable: false, comment: "FK a la unidad de medida de esta presentación (ej: LATA400, BOT160, KG)."),
                    conversionFactor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Cuántas unidades base equivalen a 1 de esta presentación. La fila base siempre vale 1.000000."),
                    isBase = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Exactamente 1 registro por producto marca la unidad base (isBase=1, conversionFactor=1, idUnit = product.idUnit)."),
                    usedForPurchase = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si esta presentación puede usarse en líneas de factura de compra."),
                    usedForSale = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si esta presentación puede usarse en líneas de factura de venta."),
                    codeBarcode = table.Column<string>(type: "varchar(48)", unicode: false, maxLength: 48, nullable: true, comment: "Código de barras EAN-8, EAN-13 o UPC-A del empaque. NULL si no tiene barcode. Único en todo el sistema."),
                    namePresentation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Nombre comercial del empaque tal como aparece en la etiqueta (ej: Cahuita Salsa Caribeña 160ml)."),
                    brandPresentation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Marca del fabricante del empaque (ej: Fiesta de Diablitos, Aroy-D)."),
                    salePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m, comment: "Precio base de venta para esta presentación. El precio final en combos/opciones se calcula sumando deltas.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productUnit", x => x.idProductUnit);
                    table.ForeignKey(
                        name: "FK_productUnit_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productUnit_unitOfMeasure_idUnit",
                        column: x => x.idUnit,
                        principalTable: "unitOfMeasure",
                        principalColumn: "idUnit",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Presentaciones de compra/venta de un producto con su factor de conversión a la unidad base. Reemplaza productSKU + productProductSKU. El campo codeBarcode permite escanear EAN para pre-llenar líneas de factura.");

            migrationBuilder.CreateTable(
                name: "purchaseInvoice",
                columns: table => new
                {
                    idPurchaseInvoice = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la factura de compra.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que pertenece la factura de compra."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda de la factura. Para tipo EFECTIVO determina qué cuenta Caja usar (CRC o USD)."),
                    idPurchaseInvoiceType = table.Column<int>(type: "int", nullable: false, comment: "FK al tipo de factura de compra (EFECTIVO, DEBITO, TC)."),
                    idBankAccount = table.Column<int>(type: "int", nullable: true),
                    numberInvoice = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número de factura tal como aparece en el documento del proveedor."),
                    providerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre del proveedor (ingreso libre, sin catálogo en esta fase)."),
                    dateInvoice = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de emisión de la factura del proveedor."),
                    subTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Subtotal de la factura antes de impuestos."),
                    taxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total de impuestos de la factura."),
                    totalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total general de la factura (SubTotalAmount + TaxAmount)."),
                    statusInvoice = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, defaultValue: "Borrador", comment: "Estado de la factura: 'Borrador', 'Confirmado' o 'Anulado'."),
                    descriptionInvoice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Notas adicionales opcionales sobre la factura de compra."),
                    exchangeRateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Tipo de cambio vigente al momento del registro de la factura."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()", comment: "Fecha y hora de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseInvoice", x => x.idPurchaseInvoice);
                    table.CheckConstraint("CK_purchaseInvoice_statusInvoice", "statusInvoice IN ('Borrador', 'Confirmado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_purchaseInvoice_bankAccount_idBankAccount",
                        column: x => x.idBankAccount,
                        principalTable: "bankAccount",
                        principalColumn: "idBankAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchaseInvoice_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchaseInvoice_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchaseInvoice_purchaseInvoiceType_idPurchaseInvoiceType",
                        column: x => x.idPurchaseInvoiceType,
                        principalTable: "purchaseInvoiceType",
                        principalColumn: "idPurchaseInvoiceType",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Cabecera de factura de compra. Registra el gasto y genera automáticamente un asiento contable al confirmar.");

            migrationBuilder.CreateTable(
                name: "inventoryAdjustmentEntry",
                columns: table => new
                {
                    idInventoryAdjustmentEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del vínculo ajuste-asiento.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: false, comment: "FK al ajuste de inventario."),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false, comment: "FK al asiento contable vinculado al ajuste.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryAdjustmentEntry", x => x.idInventoryAdjustmentEntry);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustmentEntry_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustmentEntry_inventoryAdjustment_idInventoryAdjustment",
                        column: x => x.idInventoryAdjustment,
                        principalTable: "inventoryAdjustment",
                        principalColumn: "idInventoryAdjustment",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla auxiliar N:M entre inventoryAdjustment y accountingEntry. Un ajuste puede vincularse a más de un asiento: el asiento inicial de confirmación y cualquier asiento de reversión posterior. Nunca se modifica un asiento confirmado; se agregan nuevas filas en esta tabla.");

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

            migrationBuilder.CreateTable(
                name: "productComboSlotProduct",
                columns: table => new
                {
                    idProductComboSlotProduct = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "FK al slot del combo al que pertenece esta opción."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto permitido en este slot."),
                    priceAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m, comment: "Ajuste adicional al precio del combo por elegir este producto en el slot."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación dentro del slot.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productComboSlotProduct", x => x.idProductComboSlotProduct);
                    table.ForeignKey(
                        name: "FK_productComboSlotProduct_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productComboSlotProduct_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Productos permitidos en cada slot de un combo. El cliente elige uno de esta lista al armar el pedido.");

            migrationBuilder.CreateTable(
                name: "productOptionItem",
                columns: table => new
                {
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del item de opción.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductOptionGroup = table.Column<int>(type: "int", nullable: false, comment: "FK al grupo de opciones al que pertenece este item."),
                    nameItem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre visible de la opción (ej: Masa Delgada)."),
                    priceDelta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m, comment: "Ajuste de precio sobre el precio base del producto. Puede ser positivo, negativo o cero."),
                    isDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Opción marcada por defecto al abrir el selector."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación dentro del grupo.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productOptionItem", x => x.idProductOptionItem);
                    table.ForeignKey(
                        name: "FK_productOptionItem_productOptionGroup_idProductOptionGroup",
                        column: x => x.idProductOptionGroup,
                        principalTable: "productOptionGroup",
                        principalColumn: "idProductOptionGroup",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Cada opción dentro de un grupo configurable (ej: Delgada, Gruesa, Rellena dentro del grupo Masa).");

            migrationBuilder.CreateTable(
                name: "productRecipeLine",
                columns: table => new
                {
                    idProductRecipeLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea de receta.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductRecipe = table.Column<int>(type: "int", nullable: false, comment: "FK a la receta cabecera. Cascade delete."),
                    idProductInput = table.Column<int>(type: "int", nullable: false, comment: "FK al producto insumo. No puede ser igual al idProductOutput de la receta."),
                    quantityInput = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad requerida del insumo en su unidad base por cada corrida de la receta."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de visualización de los ingredientes.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productRecipeLine", x => x.idProductRecipeLine);
                    table.ForeignKey(
                        name: "FK_productRecipeLine_productRecipe_idProductRecipe",
                        column: x => x.idProductRecipe,
                        principalTable: "productRecipe",
                        principalColumn: "idProductRecipe",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productRecipeLine_product_idProductInput",
                        column: x => x.idProductInput,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Ingredientes de una receta de producción. Cada línea es un insumo con su cantidad en unidad base. idProductInput no puede ser igual al idProductOutput de la receta padre.");

            migrationBuilder.CreateTable(
                name: "bankMovementDocument",
                columns: table => new
                {
                    idBankMovementDocument = table.Column<int>(type: "int", nullable: false, comment: "Identificador único del documento de soporte")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idBankMovement = table.Column<int>(type: "int", nullable: false, comment: "FK al movimiento bancario al que pertenece el documento"),
                    idPurchaseInvoice = table.Column<int>(type: "int", nullable: true, comment: "FK opcional a la factura de compra vinculada a este documento de soporte"),
                    typeDocument = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Tipo de documento: 'FacturaCompra', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'"),
                    numberDocument = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true, comment: "Número o referencia del documento (factura, cheque, etc.)"),
                    dateDocument = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del documento de soporte"),
                    amountDocument = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto del documento de soporte"),
                    descriptionDocument = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Descripción adicional del documento")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankMovementDocument", x => x.idBankMovementDocument);
                    table.CheckConstraint("CK_bankMovementDocument_typeDocument", "typeDocument IN ('FacturaCompra', 'Recibo', 'Transferencia', 'Cheque', 'Otro')");
                    table.ForeignKey(
                        name: "FK_bankMovementDocument_bankMovement_idBankMovement",
                        column: x => x.idBankMovement,
                        principalTable: "bankMovement",
                        principalColumn: "idBankMovement",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bankMovementDocument_purchaseInvoice_idPurchaseInvoice",
                        column: x => x.idPurchaseInvoice,
                        principalTable: "purchaseInvoice",
                        principalColumn: "idPurchaseInvoice",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Documentos de soporte vinculados a un movimiento bancario");

            migrationBuilder.CreateTable(
                name: "inventoryLot",
                columns: table => new
                {
                    idInventoryLot = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del lote.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto de este lote."),
                    lotNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, comment: "Número de lote: '{idContact}-{numberInvoice}' para compras, código interno para producción, 'SYSTEM-{idInventoryAdjustment}' para ajustes. NULL si no aplica."),
                    expirationDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha de vencimiento del lote. NULL para productos no perecederos."),
                    unitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 0m, comment: "Costo unitario en unidad base al momento del ingreso del lote."),
                    quantityAvailable = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 0m, comment: "Stock disponible en unidad base. Solo se modifica al confirmar documentos. Nunca editable directamente."),
                    sourceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Origen del lote: Compra | Producción | Ajuste."),
                    idPurchaseInvoice = table.Column<int>(type: "int", nullable: true, comment: "FK a la factura de compra que originó este lote. Poblado si sourceType = 'Compra'."),
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: true, comment: "FK al ajuste de inventario que originó este lote. Poblado si sourceType = 'Ajuste' o 'Producción' (V1)."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryLot", x => x.idInventoryLot);
                    table.CheckConstraint("CK_inventoryLot_quantityAvailable", "quantityAvailable >= 0");
                    table.CheckConstraint("CK_inventoryLot_sourceType", "sourceType IN ('Compra', 'Producción', 'Ajuste')");
                    table.ForeignKey(
                        name: "FK_inventoryLot_inventoryAdjustment_idInventoryAdjustment",
                        column: x => x.idInventoryAdjustment,
                        principalTable: "inventoryAdjustment",
                        principalColumn: "idInventoryAdjustment",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inventoryLot_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventoryLot_purchaseInvoice_idPurchaseInvoice",
                        column: x => x.idPurchaseInvoice,
                        principalTable: "purchaseInvoice",
                        principalColumn: "idPurchaseInvoice",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Registro de stock de inventario por producto y lote. Es la unidad mínima de trazabilidad. quantityAvailable nunca se edita directamente: solo se modifica al confirmar purchaseInvoice, salesInvoice o inventoryAdjustment.");

            migrationBuilder.CreateTable(
                name: "purchaseInvoiceEntry",
                columns: table => new
                {
                    idPurchaseInvoiceEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del vínculo factura-asiento.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idPurchaseInvoice = table.Column<int>(type: "int", nullable: false, comment: "FK a la factura de compra."),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false, comment: "FK al asiento contable vinculado a la factura.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseInvoiceEntry", x => x.idPurchaseInvoiceEntry);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceEntry_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceEntry_purchaseInvoice_idPurchaseInvoice",
                        column: x => x.idPurchaseInvoice,
                        principalTable: "purchaseInvoice",
                        principalColumn: "idPurchaseInvoice",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla auxiliar N:M entre purchaseInvoice y accountingEntry. Una factura puede vincularse a más de un asiento: el asiento inicial de confirmación y cualquier asiento de ajuste posterior. Nunca se modifica un asiento confirmado; se agregan nuevas filas en esta tabla.");

            migrationBuilder.CreateTable(
                name: "purchaseInvoiceLine",
                columns: table => new
                {
                    idPurchaseInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea de factura de compra.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idPurchaseInvoice = table.Column<int>(type: "int", nullable: false, comment: "FK a la factura de compra cabecera. Cascade delete."),
                    idProduct = table.Column<int>(type: "int", nullable: true, comment: "FK al producto. NULL para líneas de gasto sin producto (flete, servicios, etc.)."),
                    idUnit = table.Column<int>(type: "int", nullable: true, comment: "FK a la unidad de medida de la compra. Debe existir en productUnit para el idProduct con usedForPurchase=1. NULL si idProduct es NULL."),
                    descriptionLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "Descripción de la línea tal como aparece en la factura del proveedor."),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad comprada en la unidad idUnit."),
                    quantityBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true, comment: "Cantidad en unidad base del producto: quantity × productUnit.conversionFactor. Calculado al confirmar la factura. No editable."),
                    unitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Precio unitario del producto o servicio en la unidad idUnit."),
                    taxPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "Porcentaje de impuesto aplicado a la línea (ej: 13.00 para IVA 13%)."),
                    totalLineAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Total de la línea: quantity × unitPrice × (1 + taxPercent / 100)."),
                    lotNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, comment: "Número de lote del proveedor impreso en la etiqueta física del insumo. Pasa a inventoryLot.lotNumber al confirmar."),
                    expirationDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha de vencimiento según la etiqueta del proveedor. Pasa a inventoryLot.expirationDate al confirmar. NULL para productos no perecederos.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseInvoiceLine", x => x.idPurchaseInvoiceLine);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceLine_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceLine_purchaseInvoice_idPurchaseInvoice",
                        column: x => x.idPurchaseInvoice,
                        principalTable: "purchaseInvoice",
                        principalColumn: "idPurchaseInvoice",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceLine_unitOfMeasure_idUnit",
                        column: x => x.idUnit,
                        principalTable: "unitOfMeasure",
                        principalColumn: "idUnit",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Líneas de la factura de compra. Cada línea representa un producto o servicio adquirido. Cuando idProduct está presente, al confirmar la factura se crea automáticamente un registro en inventoryLot y se recalcula product.averageCost.");

            migrationBuilder.CreateTable(
                name: "inventoryAdjustmentLine",
                columns: table => new
                {
                    idInventoryAdjustmentLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: false, comment: "FK al ajuste de inventario cabecera. Cascade delete."),
                    idInventoryLot = table.Column<int>(type: "int", nullable: false, comment: "FK al lote de inventario a ajustar. Para líneas positivas que crean un lote nuevo, se crea el lote primero."),
                    quantityDelta = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro (no mueve stock)."),
                    unitCostNew = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true, comment: "Nuevo costo unitario para el lote. Requerido si quantityDelta > 0. Si informado: reemplaza inventoryLot.unitCost."),
                    descriptionLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Detalle por línea: insumo consumido, merma, motivo del ajuste, etc.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryAdjustmentLine", x => x.idInventoryAdjustmentLine);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustmentLine_inventoryAdjustment_idInventoryAdjustment",
                        column: x => x.idInventoryAdjustment,
                        principalTable: "inventoryAdjustment",
                        principalColumn: "idInventoryAdjustment",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventoryAdjustmentLine_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Líneas del ajuste de inventario. Cada línea referencia un lote específico: quantityDelta positivo = entrada, negativo = salida, cero = ajuste de costo puro. Si quantityDelta > 0, unitCostNew es requerido.");

            migrationBuilder.CreateTable(
                name: "purchaseInvoiceLineEntry",
                columns: table => new
                {
                    idPurchaseInvoiceLineEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del vínculo línea-factura/línea-asiento.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idPurchaseInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea de factura de compra."),
                    idAccountingEntryLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea del asiento contable generada a partir de esta línea de factura.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseInvoiceLineEntry", x => x.idPurchaseInvoiceLineEntry);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceLineEntry_accountingEntryLine_idAccountingEntryLine",
                        column: x => x.idAccountingEntryLine,
                        principalTable: "accountingEntryLine",
                        principalColumn: "idAccountingEntryLine",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchaseInvoiceLineEntry_purchaseInvoiceLine_idPurchaseInvoiceLine",
                        column: x => x.idPurchaseInvoiceLine,
                        principalTable: "purchaseInvoiceLine",
                        principalColumn: "idPurchaseInvoiceLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla auxiliar N:M entre purchaseInvoiceLine y accountingEntryLine. Permite trazar qué líneas del asiento contable se originaron de cada línea de factura. Una línea de factura genera N líneas contables según la distribución de ProductAccount.");

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
                values: new object[] { 1, "Banco de Costa Rica", "BCR-HTML-XLS-V1", "{\"accountingDate\":0,\"transactionDate\":1,\"transactionTime\":2,\"documentNumber\":3,\"description\":4,\"debitAmount\":5,\"creditAmount\":6,\"balance\":7,\"skipHeaderRows\":1}", "dd/MM/yyyy", true, "[\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\n                                                                        \"idBankMovementType\":1,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"DEP EFECTIVO\",\"DEPOSITO EFECTIVO\",\"DEPOSITO EN CAJA\"],\n                                                                        \"idBankMovementType\":2,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"INTERNET DTR SINPE\",\"DTR SINPE\",\"SINPE CR\",\"TRANSF CREDIT\",\"CREDITO SINPE\",\"SINPE MOVIL CR\",\"ABONO SINPE\",\"RECIBO SINPE\"],\n                                                                        \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"COMPRAS EN COMERCIOS\",\"COMPRA EN COMERCIO\",\"COMPRAS COMERC\",\"COMPRA COMERC\"],\n                                                                        \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"RETIRO ATM\",\"RETIRO CAJERO\",\"RETIRO EFECTIVO\",\"CAJERO AUTOMATICO\"],\n                                                                        \"idBankMovementType\":5,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO TC\",\"PAGO TARJETA\",\"TRJ CRED\",\"PAGO TARJETA CREDITO\",\"PAGO TRJ\",\"PAGO TARJETAS\"],\n                                                                        \"idBankMovementType\":6,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO PREST\",\"CUOTA PREST\",\"PAGO PRESTAMO\",\"CUOTA PRESTAMO\"],\n                                                                        \"idBankMovementType\":7,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"SINPE MOVIL OTRA ENT\",\"OTRA ENT\",\"TRANSF DEB\",\"SINPE DEB\",\"DEB SINPE\",\"SINPE MOVIL DEB\",\"DEBITO SINPE\",\"TRANSFERENCIA SINPE DEB\",\"CARGO SINPE\"],\n                                                                        \"idBankMovementType\":8,\"matchMode\":\"Any\"}\n]", "BCR – Movimientos de Cuenta (HTML-XLS)", "Archivo exportado como .xls desde el portal BCR. El contenido real es HTML con una tabla id='t1'. Aplica para cuentas de ahorros y cuentas corrientes en colones y dólares.", "HH:mm:ss" });

            migrationBuilder.InsertData(
                table: "company",
                columns: new[] { "idCompany", "codeCompany", "nameCompany" },
                values: new object[,]
                {
                    { 1, "FBS", "Familia Baltodano Soto" },
                    { 2, "CDSRL", "Corporacion los diablitos SRL" }
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
                table: "productType",
                columns: new[] { "idProductType", "descriptionProductType", "nameProductType" },
                values: new object[,]
                {
                    { 1, "Insumos o materiales adquiridos para ser utilizados en el proceso productivo. No se venden directamente.", "Materia Prima" },
                    { 2, "Productos que han iniciado su proceso de fabricación pero aún no están terminados.", "Producto en Proceso" },
                    { 3, "Productos que han completado el proceso productivo y están listos para la venta.", "Producto Terminado" },
                    { 4, "Productos adquiridos listos para la venta sin transformación productiva.", "Reventa" }
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
                table: "unitOfMeasure",
                columns: new[] { "idUnit", "codeUnit", "nameUnit", "typeUnit" },
                values: new object[,]
                {
                    { 1, "U", "Unidad", "Unidad" },
                    { 2, "M3", "Metro Cúbico", "Volumen" },
                    { 3, "KG", "Kilogramo", "Masa" },
                    { 4, "M", "Metro", "Longitud" }
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
                    { 103, false, "5.13", 5, true, 2, "Gastos Financieros", "Gasto" },
                    { 112, false, "5.14", 5, true, 2, "Ajustes de Inventario", "Gasto" }
                });

            migrationBuilder.InsertData(
                table: "companyDomain",
                columns: new[] { "idDomain", "domainUrl", "idCompany" },
                values: new object[,]
                {
                    { 1, "localhost:8000", 1 },
                    { 2, "localhost:8001", 1 },
                    { 3, "diablitos.ezekl.com", 2 }
                });

            migrationBuilder.InsertData(
                table: "companyWhatsapp",
                columns: new[] { "idCompanyWhatsapp", "accessToken", "apiVersion", "idCompany", "isActive", "phoneNumber", "phoneNumberId", "wabaId", "webhookVerifyToken" },
                values: new object[] { 1, "EAAvhrvgZBWCQBOZBxsB7YHZCNISSIugqpkPDDG6UZCgQv0AFqHFE9BtT7tXYlTygFDfJ3BhlCFAAPD6Pu7rVsI0orXxhxsMvDqsCF3alYbU9T8CYQQCzViv6Rck94yHkYr7ueiJLL4M4XLax46rLyULdZBwESpW5TvKoS6UDnS9byoZA73gM8BAHDgd3KZCcpPo", "v24.0", 2, true, "+15550636204", "102981099397560", "110007718685670", "mi_token_secreto_whatsapp_2024" });

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
                    { 104, true, "5.13.01", 103, true, 3, "Diferencial Cambiario Desfavorable", "Gasto" },
                    { 105, false, "1.1.06", 7, true, 3, "Caja / Efectivo", "Activo" },
                    { 108, false, "1.1.07", 7, true, 3, "Inventario", "Activo" },
                    { 113, true, "5.14.01", 112, true, 3, "Faltantes de Inventario (Merma)", "Gasto" },
                    { 114, true, "5.14.02", 112, true, 3, "Sobrantes de Inventario", "Gasto" },
                    { 115, true, "5.14.03", 112, true, 3, "Costos de Producción", "Gasto" }
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
                    { 55, true, "1.1.05.01", 54, true, 4, "Davivienda - AHO CR98010401446613244113 (₡) - Baltodano Cubillo Ezequiel [Nómina ITQS]", "Activo" },
                    { 106, true, "1.1.06.01", 105, true, 4, "Caja CRC (₡)", "Activo" },
                    { 107, true, "1.1.06.02", 105, true, 4, "Caja USD ($)", "Activo" },
                    { 109, true, "1.1.07.01", 108, true, 4, "Inventario de Mercadería", "Activo" },
                    { 110, true, "1.1.07.02", 108, true, 4, "Materias Primas", "Activo" },
                    { 111, true, "1.1.07.03", 108, true, 4, "Productos en Proceso", "Activo" }
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
                table: "purchaseInvoiceType",
                columns: new[] { "idPurchaseInvoiceType", "codePurchaseInvoiceType", "counterpartFromBankMovement", "idAccountCounterpartCRC", "idAccountCounterpartUSD", "idBankMovementType", "idDefaultExpenseAccount", "isActive", "namePurchaseInvoiceType" },
                values: new object[] { 2, "DEBITO", true, null, null, 4, 75, true, "Tarjeta de Débito / Transferencia" });

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

            migrationBuilder.InsertData(
                table: "inventoryAdjustmentType",
                columns: new[] { "idInventoryAdjustmentType", "codeInventoryAdjustmentType", "idAccountCounterpartEntry", "idAccountCounterpartExit", "idAccountInventoryDefault", "isActive", "nameInventoryAdjustmentType" },
                values: new object[,]
                {
                    { 1, "CONTEO", 114, 113, 109, true, "Conteo Físico" },
                    { 2, "PRODUCCION", 115, 115, 111, true, "Producción" },
                    { 3, "AJUSTE_COSTO", 114, 113, 109, true, "Ajuste de Costo" }
                });

            migrationBuilder.InsertData(
                table: "purchaseInvoiceType",
                columns: new[] { "idPurchaseInvoiceType", "codePurchaseInvoiceType", "counterpartFromBankMovement", "idAccountCounterpartCRC", "idAccountCounterpartUSD", "idBankMovementType", "idDefaultExpenseAccount", "isActive", "namePurchaseInvoiceType" },
                values: new object[,]
                {
                    { 1, "EFECTIVO", false, 106, 107, null, 75, true, "Pago en Efectivo" },
                    { 3, "TC", true, null, null, 6, 75, true, "Tarjeta de Crédito" }
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
                name: "IX_accountingEntry_originModule_idOriginRecord",
                table: "accountingEntry",
                columns: new[] { "originModule", "idOriginRecord" },
                filter: "[originModule] IS NOT NULL");

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
                name: "IX_bankMovement_idAccountingEntry",
                table: "bankMovement",
                column: "idAccountingEntry",
                filter: "[idAccountingEntry] IS NOT NULL");

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
                name: "IX_bankMovementDocument_idBankMovement",
                table: "bankMovementDocument",
                column: "idBankMovement");

            migrationBuilder.CreateIndex(
                name: "IX_bankMovementDocument_idPurchaseInvoice",
                table: "bankMovementDocument",
                column: "idPurchaseInvoice",
                filter: "[idPurchaseInvoice] IS NOT NULL");

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
                name: "UQ_company_codeCompany",
                table: "company",
                column: "codeCompany",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companyDomain_idCompany",
                table: "companyDomain",
                column: "idCompany");

            migrationBuilder.CreateIndex(
                name: "UQ_domain_domain",
                table: "companyDomain",
                column: "domainUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_companyWhatsapp_idCompany",
                table: "companyWhatsapp",
                column: "idCompany",
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
                name: "IX_inventoryAdjustment_idCurrency",
                table: "inventoryAdjustment",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustment_idFiscalPeriod",
                table: "inventoryAdjustment",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustment_idInventoryAdjustmentType",
                table: "inventoryAdjustment",
                column: "idInventoryAdjustmentType");

            migrationBuilder.CreateIndex(
                name: "UQ_inventoryAdjustment_numberAdjustment",
                table: "inventoryAdjustment",
                column: "numberAdjustment",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustmentEntry_idAccountingEntry",
                table: "inventoryAdjustmentEntry",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "UQ_inventoryAdjustmentEntry_idInventoryAdjustment_idAccountingEntry",
                table: "inventoryAdjustmentEntry",
                columns: new[] { "idInventoryAdjustment", "idAccountingEntry" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustmentLine_idInventoryAdjustment",
                table: "inventoryAdjustmentLine",
                column: "idInventoryAdjustment");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustmentLine_idInventoryLot",
                table: "inventoryAdjustmentLine",
                column: "idInventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustmentType_idAccountCounterpartEntry",
                table: "inventoryAdjustmentType",
                column: "idAccountCounterpartEntry",
                filter: "[idAccountCounterpartEntry] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustmentType_idAccountCounterpartExit",
                table: "inventoryAdjustmentType",
                column: "idAccountCounterpartExit",
                filter: "[idAccountCounterpartExit] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryAdjustmentType_idAccountInventoryDefault",
                table: "inventoryAdjustmentType",
                column: "idAccountInventoryDefault",
                filter: "[idAccountInventoryDefault] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_inventoryAdjustmentType_codeInventoryAdjustmentType",
                table: "inventoryAdjustmentType",
                column: "codeInventoryAdjustmentType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventoryLot_idInventoryAdjustment",
                table: "inventoryLot",
                column: "idInventoryAdjustment");

            migrationBuilder.CreateIndex(
                name: "IX_inventoryLot_idProduct_expirationDate",
                table: "inventoryLot",
                columns: new[] { "idProduct", "expirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_inventoryLot_idPurchaseInvoice",
                table: "inventoryLot",
                column: "idPurchaseInvoice",
                filter: "[idPurchaseInvoice] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_idProductParent",
                table: "product",
                column: "idProductParent");

            migrationBuilder.CreateIndex(
                name: "IX_product_idProductType",
                table: "product",
                column: "idProductType");

            migrationBuilder.CreateIndex(
                name: "IX_product_idUnit",
                table: "product",
                column: "idUnit");

            migrationBuilder.CreateIndex(
                name: "UQ_product_codeProduct",
                table: "product",
                column: "codeProduct",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productAccount_idAccount",
                table: "productAccount",
                column: "idAccount");

            migrationBuilder.CreateIndex(
                name: "IX_productAccount_idCostCenter",
                table: "productAccount",
                column: "idCostCenter",
                filter: "[idCostCenter] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_productAccount_productIdProduct",
                table: "productAccount",
                column: "productIdProduct");

            migrationBuilder.CreateIndex(
                name: "UQ_productAccount_idProduct_idAccount_idCostCenter",
                table: "productAccount",
                columns: new[] { "idProduct", "idAccount", "idCostCenter" },
                unique: true,
                filter: "[idCostCenter] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_productComboSlot_idProductCombo",
                table: "productComboSlot",
                column: "idProductCombo");

            migrationBuilder.CreateIndex(
                name: "IX_productComboSlotProduct_idProduct",
                table: "productComboSlotProduct",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "UQ_productComboSlotProduct_idSlot_idProduct",
                table: "productComboSlotProduct",
                columns: new[] { "idProductComboSlot", "idProduct" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productOptionGroup_idProduct",
                table: "productOptionGroup",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_productOptionItem_idProductOptionGroup",
                table: "productOptionItem",
                column: "idProductOptionGroup");

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
                name: "IX_productRecipe_idProductOutput",
                table: "productRecipe",
                column: "idProductOutput");

            migrationBuilder.CreateIndex(
                name: "IX_productRecipeLine_idProductInput",
                table: "productRecipeLine",
                column: "idProductInput");

            migrationBuilder.CreateIndex(
                name: "IX_productRecipeLine_idProductRecipe",
                table: "productRecipeLine",
                column: "idProductRecipe");

            migrationBuilder.CreateIndex(
                name: "UQ_productType_nameProductType",
                table: "productType",
                column: "nameProductType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productUnit_idUnit",
                table: "productUnit",
                column: "idUnit");

            migrationBuilder.CreateIndex(
                name: "UQ_productUnit_codeBarcode",
                table: "productUnit",
                column: "codeBarcode",
                unique: true,
                filter: "[codeBarcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_productUnit_idProduct_idUnit",
                table: "productUnit",
                columns: new[] { "idProduct", "idUnit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idBankAccount",
                table: "purchaseInvoice",
                column: "idBankAccount");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idCurrency",
                table: "purchaseInvoice",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idFiscalPeriod",
                table: "purchaseInvoice",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idPurchaseInvoiceType",
                table: "purchaseInvoice",
                column: "idPurchaseInvoiceType");

            migrationBuilder.CreateIndex(
                name: "UQ_purchaseInvoice_numberInvoice",
                table: "purchaseInvoice",
                column: "numberInvoice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceEntry_idAccountingEntry",
                table: "purchaseInvoiceEntry",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "UQ_purchaseInvoiceEntry_idPurchaseInvoice_idAccountingEntry",
                table: "purchaseInvoiceEntry",
                columns: new[] { "idPurchaseInvoice", "idAccountingEntry" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceLine_idProduct",
                table: "purchaseInvoiceLine",
                column: "idProduct",
                filter: "[idProduct] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceLine_idPurchaseInvoice",
                table: "purchaseInvoiceLine",
                column: "idPurchaseInvoice");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceLine_idUnit",
                table: "purchaseInvoiceLine",
                column: "idUnit");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceLineEntry_idAccountingEntryLine",
                table: "purchaseInvoiceLineEntry",
                column: "idAccountingEntryLine");

            migrationBuilder.CreateIndex(
                name: "UQ_purchaseInvoiceLineEntry_idPurchaseInvoiceLine_idAccountingEntryLine",
                table: "purchaseInvoiceLineEntry",
                columns: new[] { "idPurchaseInvoiceLine", "idAccountingEntryLine" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceType_idAccountCounterpartCRC",
                table: "purchaseInvoiceType",
                column: "idAccountCounterpartCRC",
                filter: "[idAccountCounterpartCRC] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceType_idAccountCounterpartUSD",
                table: "purchaseInvoiceType",
                column: "idAccountCounterpartUSD",
                filter: "[idAccountCounterpartUSD] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceType_idBankMovementType",
                table: "purchaseInvoiceType",
                column: "idBankMovementType",
                filter: "[idBankMovementType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoiceType_idDefaultExpenseAccount",
                table: "purchaseInvoiceType",
                column: "idDefaultExpenseAccount",
                filter: "[idDefaultExpenseAccount] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_purchaseInvoiceType_codePurchaseInvoiceType",
                table: "purchaseInvoiceType",
                column: "codePurchaseInvoiceType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_role_nameRole",
                table: "role",
                column: "nameRole",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_unitOfMeasure_codeUnit",
                table: "unitOfMeasure",
                column: "codeUnit",
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
                name: "bankMovementDocument");

            migrationBuilder.DropTable(
                name: "bankStatementTransaction");

            migrationBuilder.DropTable(
                name: "budget");

            migrationBuilder.DropTable(
                name: "companyDomain");

            migrationBuilder.DropTable(
                name: "companyWhatsapp");

            migrationBuilder.DropTable(
                name: "contactContactType");

            migrationBuilder.DropTable(
                name: "exchangeRate");

            migrationBuilder.DropTable(
                name: "inventoryAdjustmentEntry");

            migrationBuilder.DropTable(
                name: "inventoryAdjustmentLine");

            migrationBuilder.DropTable(
                name: "productAccount");

            migrationBuilder.DropTable(
                name: "productComboSlotProduct");

            migrationBuilder.DropTable(
                name: "productOptionItem");

            migrationBuilder.DropTable(
                name: "productProductCategory");

            migrationBuilder.DropTable(
                name: "productRecipeLine");

            migrationBuilder.DropTable(
                name: "productUnit");

            migrationBuilder.DropTable(
                name: "purchaseInvoiceEntry");

            migrationBuilder.DropTable(
                name: "purchaseInvoiceLineEntry");

            migrationBuilder.DropTable(
                name: "userPin");

            migrationBuilder.DropTable(
                name: "userRole");

            migrationBuilder.DropTable(
                name: "bankMovement");

            migrationBuilder.DropTable(
                name: "bankStatementImport");

            migrationBuilder.DropTable(
                name: "company");

            migrationBuilder.DropTable(
                name: "contactType");

            migrationBuilder.DropTable(
                name: "contact");

            migrationBuilder.DropTable(
                name: "inventoryLot");

            migrationBuilder.DropTable(
                name: "productComboSlot");

            migrationBuilder.DropTable(
                name: "productOptionGroup");

            migrationBuilder.DropTable(
                name: "productCategory");

            migrationBuilder.DropTable(
                name: "productRecipe");

            migrationBuilder.DropTable(
                name: "accountingEntryLine");

            migrationBuilder.DropTable(
                name: "purchaseInvoiceLine");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "bankStatementTemplate");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "inventoryAdjustment");

            migrationBuilder.DropTable(
                name: "accountingEntry");

            migrationBuilder.DropTable(
                name: "costCenter");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "purchaseInvoice");

            migrationBuilder.DropTable(
                name: "inventoryAdjustmentType");

            migrationBuilder.DropTable(
                name: "productType");

            migrationBuilder.DropTable(
                name: "unitOfMeasure");

            migrationBuilder.DropTable(
                name: "bankAccount");

            migrationBuilder.DropTable(
                name: "fiscalPeriod");

            migrationBuilder.DropTable(
                name: "purchaseInvoiceType");

            migrationBuilder.DropTable(
                name: "bank");

            migrationBuilder.DropTable(
                name: "currency");

            migrationBuilder.DropTable(
                name: "bankMovementType");

            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
