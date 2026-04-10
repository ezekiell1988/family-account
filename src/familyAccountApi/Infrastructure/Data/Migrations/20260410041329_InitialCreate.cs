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
                    codeBank = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Código único de la entidad bancaria. Ejemplo: BCR, BNCR, BAC, COOPEALIANZA."),
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
                name: "priceList",
                columns: table => new
                {
                    idPriceList = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la lista de precios.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    namePriceList = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre descriptivo de la lista (ej: Lista Mayorista Abril 2026)."),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Descripción opcional de la lista."),
                    dateFrom = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de inicio de vigencia."),
                    dateTo = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha de fin de vigencia. NULL = vigente indefinidamente."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si la lista está activa para su uso."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_priceList", x => x.idPriceList);
                },
                comment: "Lista de precios con vigencia por fechas. Al crear un pedido se hace snapshot del precio vigente en SalesOrderLine.UnitPrice.");

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
                    nameProductType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, comment: "Nombre del tipo: Materia Prima | Producto en Proceso | Producto Terminado | Reventa | Servicios."),
                    descriptionProductType = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "Descripción del tipo de producto y sus reglas de negocio."),
                    trackInventory = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si los productos de este tipo llevan control de stock (inventariables). false = Servicios y productos sin inventario.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productType", x => x.idProductType);
                },
                comment: "Tipo de producto según la fase productiva: Materia Prima, Producto en Proceso, Producto Terminado, Reventa o Servicios. Catálogo de sistema, sin CRUD expuesto al usuario.");

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
                name: "unitType",
                columns: table => new
                {
                    idUnitType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de unidad.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nameUnitType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, comment: "Nombre del tipo dimensional: Unidad | Volumen | Masa | Longitud.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unitType", x => x.idUnitType);
                },
                comment: "Clasificación dimensional de unidades de medida. Catálogo de sistema, sin CRUD expuesto al usuario.");

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
                name: "warehouse",
                columns: table => new
                {
                    idWarehouse = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del almacén.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nameWarehouse = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nombre descriptivo del almacén. Debe ser único."),
                    isDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Indica si este es el almacén predeterminado. Solo uno puede ser predeterminado a la vez. Se usa cuando no se especifica almacén al ingresar mercadería."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Almacén activo. Los almacenes inactivos no aceptan nuevas entradas de stock.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse", x => x.idWarehouse);
                },
                comment: "Almacenes o bodegas de la empresa. El stock de inventario se segmenta por almacén a través de InventoryLot.idWarehouse.");

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
                name: "salesOrder",
                columns: table => new
                {
                    idSalesOrder = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del pedido.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda del pedido."),
                    idContact = table.Column<int>(type: "int", nullable: false, comment: "FK al cliente que realiza el pedido."),
                    idPriceList = table.Column<int>(type: "int", nullable: true, comment: "FK a la lista de precios vigente al crear el pedido. Sirve como referencia; el precio real queda en SalesOrderLine.UnitPrice."),
                    numberOrder = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número correlativo del pedido (ej: PED-2026-0001)."),
                    dateOrder = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha en que se ingresó el pedido."),
                    dateDelivery = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha compromiso de entrega al cliente. Nullable."),
                    subTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Subtotal sin impuesto."),
                    taxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto total de impuesto."),
                    totalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total del pedido (subtotal + impuesto)."),
                    exchangeRateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Tipo de cambio al momento de crear el pedido."),
                    statusOrder = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Borrador", comment: "Estado del pedido: Borrador | Confirmado | EnProduccion | Completado | Anulado."),
                    descriptionOrder = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Observaciones opcionales del pedido."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrder", x => x.idSalesOrder);
                    table.CheckConstraint("CK_salesOrder_statusOrder", "statusOrder IN ('Borrador', 'Confirmado', 'EnProduccion', 'Completado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_salesOrder_contact_idContact",
                        column: x => x.idContact,
                        principalTable: "contact",
                        principalColumn: "idContact",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrder_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrder_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrder_priceList_idPriceList",
                        column: x => x.idPriceList,
                        principalTable: "priceList",
                        principalColumn: "idPriceList",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Pedido de un cliente externo. Modalidad B de producción: permite mezclar stock existente y órdenes de producción para cumplir el pedido. Flujo: Borrador → Confirmado → EnProduccion → Completado → Anulado.");

            migrationBuilder.CreateTable(
                name: "unitOfMeasure",
                columns: table => new
                {
                    idUnit = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la unidad de medida.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeUnit = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, comment: "Código corto de la unidad: ML, GR, KG, LTR, BOT160, LATA400, UNI, etc."),
                    nameUnit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "Nombre legible de la unidad: Mililitro, Gramo, Botella 160ml, etc."),
                    idUnitType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unitOfMeasure", x => x.idUnit);
                    table.ForeignKey(
                        name: "FK_unitOfMeasure_unitType",
                        column: x => x.idUnitType,
                        principalTable: "unitType",
                        principalColumn: "idUnitType",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo global de unidades de medida utilizadas en productos, recetas e inventario.");

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
                    idDefaultInventoryAccount = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta contable de inventario (DR) usada por defecto al confirmar líneas con producto. Si el producto tiene ProductAccount configurado, esa cuenta de gasto tendrá prioridad."),
                    idDefaultExpenseAccount = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta contable de gasto alternativa. Solo se usa cuando el producto tiene un ProductAccount explícito que apunta a ella (override de cuenta de gasto en lugar de inventario)."),
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
                        name: "FK_purchaseInvoiceType_account_idDefaultInventoryAccount",
                        column: x => x.idDefaultInventoryAccount,
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
                name: "salesInvoiceType",
                columns: table => new
                {
                    idSalesInvoiceType = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del tipo de factura de venta.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codeSalesInvoiceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Código único del tipo: 'CONTADO_CRC', 'CONTADO_USD', 'CREDITO_CRC', 'CREDITO_USD'."),
                    nameSalesInvoiceType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, comment: "Nombre descriptivo del tipo de factura de venta."),
                    counterpartFromBankMovement = table.Column<bool>(type: "bit", nullable: false, comment: "true = la cuenta DR del asiento proviene del BankMovement vinculado; false = cuenta Caja fija por moneda."),
                    idAccountCounterpartCRC = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta Caja CRC. Solo aplica cuando CounterpartFromBankMovement = false."),
                    idAccountCounterpartUSD = table.Column<int>(type: "int", nullable: true, comment: "FK a la cuenta Caja USD. Solo aplica cuando CounterpartFromBankMovement = false."),
                    idBankMovementType = table.Column<int>(type: "int", nullable: true, comment: "FK al tipo de movimiento bancario para auto-crear el BankMovement al confirmar. Solo si CounterpartFromBankMovement = true."),
                    idAccountSalesRevenue = table.Column<int>(type: "int", nullable: true, comment: "Cuenta CR de ingresos por ventas (fallback cuando el producto no tiene ProductAccount configurado)."),
                    idAccountCOGS = table.Column<int>(type: "int", nullable: true, comment: "Cuenta DR de costo de ventas (COGS) al reconocer el costo."),
                    idAccountInventory = table.Column<int>(type: "int", nullable: true, comment: "Cuenta CR de inventario al reconocer el costo de ventas."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si el tipo de factura de venta está activo.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceType", x => x.idSalesInvoiceType);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountCOGS",
                        column: x => x.idAccountCOGS,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountCounterpartCRC",
                        column: x => x.idAccountCounterpartCRC,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountCounterpartUSD",
                        column: x => x.idAccountCounterpartUSD,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountInventory",
                        column: x => x.idAccountInventory,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_account_idAccountSalesRevenue",
                        column: x => x.idAccountSalesRevenue,
                        principalTable: "account",
                        principalColumn: "idAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceType_bankMovementType_idBankMovementType",
                        column: x => x.idBankMovementType,
                        principalTable: "bankMovementType",
                        principalColumn: "idBankMovementType",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo de tipos de factura de venta. Define contrapartida (Caja o BankMovement) y cuentas contables predeterminadas para ingresos y COGS.");

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
                name: "productionOrder",
                columns: table => new
                {
                    idProductionOrder = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la orden de producción.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que corresponde la orden."),
                    idSalesOrder = table.Column<int>(type: "int", nullable: true, comment: "FK al pedido de venta que origina esta orden. NULL = producción para stock (Modalidad A)."),
                    numberProductionOrder = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número correlativo de la orden (ej: OP-2026-0001)."),
                    dateOrder = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de creación de la orden de producción."),
                    dateRequired = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha en que se necesita tener disponible lo producido. Nullable."),
                    statusProductionOrder = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Borrador", comment: "Estado: Borrador | Pendiente | EnProceso | Completado | Anulado."),
                    descriptionOrder = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Observaciones opcionales."),
                    idWarehouse = table.Column<int>(type: "int", nullable: true, comment: "Bodega de producción: consumo de materias primas y entrada del producto terminado."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productionOrder", x => x.idProductionOrder);
                    table.CheckConstraint("CK_productionOrder_statusProductionOrder", "statusProductionOrder IN ('Borrador', 'Pendiente', 'EnProceso', 'Completado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_productionOrder_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionOrder_salesOrder_idSalesOrder",
                        column: x => x.idSalesOrder,
                        principalTable: "salesOrder",
                        principalColumn: "idSalesOrder",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionOrder_warehouse_idWarehouse",
                        column: x => x.idWarehouse,
                        principalTable: "warehouse",
                        principalColumn: "idWarehouse",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Orden de producción. IdSalesOrder = NULL → Modalidad A (producción para stock); IdSalesOrder NOT NULL → Modalidad B (contra pedido). Permite múltiples corridas de InventoryAdjustment tipo PRODUCCION bajo la misma orden.");

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
                    rowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false, comment: "Token de concurrencia optimista. Previene race conditions al recalcular AverageCost en confirmaciones paralelas."),
                    hasOptions = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Indica que el producto tiene grupos de opciones configurables por el cliente (ej: tamaño, masa, sabor)."),
                    isCombo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Indica que el producto es un combo compuesto de slots con productos elegibles."),
                    isVariantParent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Indica que el producto es un padre que agrupa variantes por atributos (talla, color, etc.). Los padres no tienen stock propio."),
                    reorderPoint = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true, comment: "Punto de reorden: stock mínimo que dispara una alerta de reabastecimiento. NULL si no aplica."),
                    safetyStock = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true, comment: "Stock de seguridad reservado que no debe consumirse en operación normal. NULL si no aplica."),
                    reorderQuantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true, comment: "Cantidad sugerida a pedir cuando el stock cae por debajo del punto de reorden. NULL si no aplica."),
                    classificationAbc = table.Column<string>(type: "CHAR(1)", unicode: false, maxLength: 1, nullable: true, comment: "Clasificación ABC calculada por Hangfire según valor de ventas de los últimos 90 días. A=top 80%, B=siguiente 15%, C=último 5%. NULL si sin ventas en el período.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.idProduct);
                    table.CheckConstraint("CK_product_classificationAbc", "[classificationAbc] IS NULL OR [classificationAbc] IN ('A', 'B', 'C')");
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
                name: "purchaseInvoice",
                columns: table => new
                {
                    idPurchaseInvoice = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la factura de compra.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que pertenece la factura de compra."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda de la factura. Para tipo EFECTIVO determina qué cuenta Caja usar (CRC o USD)."),
                    idPurchaseInvoiceType = table.Column<int>(type: "int", nullable: false, comment: "FK al tipo de factura de compra (EFECTIVO, DEBITO, TC)."),
                    idBankAccount = table.Column<int>(type: "int", nullable: true),
                    idContact = table.Column<int>(type: "int", nullable: true, comment: "FK al contacto proveedor. Si es nulo, el proveedor no está en el catálogo."),
                    idWarehouse = table.Column<int>(type: "int", nullable: true, comment: "FK al almacén destino de la mercadería. Opcional; si es nulo al confirmar se usa el almacén predeterminado."),
                    numberInvoice = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número de factura tal como aparece en el documento del proveedor."),
                    providerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Snapshot del nombre del proveedor en el momento de la factura. Se autocompleta desde el contacto si se envía IdContact."),
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
                        name: "FK_purchaseInvoice_contact_idContact",
                        column: x => x.idContact,
                        principalTable: "contact",
                        principalColumn: "idContact",
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
                    table.ForeignKey(
                        name: "FK_purchaseInvoice_warehouse_idWarehouse",
                        column: x => x.idWarehouse,
                        principalTable: "warehouse",
                        principalColumn: "idWarehouse",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Cabecera de factura de compra. Registra el gasto y genera automáticamente un asiento contable al confirmar.");

            migrationBuilder.CreateTable(
                name: "salesInvoice",
                columns: table => new
                {
                    idSalesInvoice = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la factura de venta.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false),
                    idCurrency = table.Column<int>(type: "int", nullable: false),
                    idSalesInvoiceType = table.Column<int>(type: "int", nullable: false),
                    idContact = table.Column<int>(type: "int", nullable: true),
                    idBankAccount = table.Column<int>(type: "int", nullable: true),
                    idSalesOrder = table.Column<int>(type: "int", nullable: true, comment: "FK al pedido de venta que origina esta factura. NULL = venta directa de tienda."),
                    numberInvoice = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, comment: "Número correlativo: FV-YYYYMMDD-NNN (asignado al confirmar; en Borrador = 'BORRADOR')."),
                    dateInvoice = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del documento de venta."),
                    subTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Subtotal sin impuesto."),
                    taxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto total de impuesto."),
                    totalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total del documento (subtotal + impuesto)."),
                    statusInvoice = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, defaultValue: "Borrador", comment: "Estado: Borrador | Confirmado | Anulado."),
                    descriptionInvoice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Observaciones opcionales del documento."),
                    exchangeRateValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Tipo de cambio vigente al momento de la venta."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoice", x => x.idSalesInvoice);
                    table.CheckConstraint("CK_salesInvoice_statusInvoice", "statusInvoice IN ('Borrador', 'Confirmado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_salesInvoice_bankAccount_idBankAccount",
                        column: x => x.idBankAccount,
                        principalTable: "bankAccount",
                        principalColumn: "idBankAccount",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_contact_idContact",
                        column: x => x.idContact,
                        principalTable: "contact",
                        principalColumn: "idContact",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_currency_idCurrency",
                        column: x => x.idCurrency,
                        principalTable: "currency",
                        principalColumn: "idCurrency",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_fiscalPeriod_idFiscalPeriod",
                        column: x => x.idFiscalPeriod,
                        principalTable: "fiscalPeriod",
                        principalColumn: "idFiscalPeriod",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_salesInvoiceType_idSalesInvoiceType",
                        column: x => x.idSalesInvoiceType,
                        principalTable: "salesInvoiceType",
                        principalColumn: "idSalesInvoiceType",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoice_salesOrder_idSalesOrder",
                        column: x => x.idSalesOrder,
                        principalTable: "salesOrder",
                        principalColumn: "idSalesOrder",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Cabecera de la factura de venta. Flujo: Borrador → Confirmado (genera asiento + COGS + descuenta lote) → Anulado (revierte).");

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
                name: "inventoryAdjustment",
                columns: table => new
                {
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del ajuste.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idFiscalPeriod = table.Column<int>(type: "int", nullable: false, comment: "FK al período fiscal al que corresponde este ajuste."),
                    idInventoryAdjustmentType = table.Column<int>(type: "int", nullable: false, comment: "FK al tipo de ajuste. Determina las cuentas contables del asiento generado al confirmar."),
                    idCurrency = table.Column<int>(type: "int", nullable: false, comment: "FK a la moneda del ajuste. Se usa en el asiento contable generado."),
                    idProductionOrder = table.Column<int>(type: "int", nullable: true, comment: "FK a la orden de produccion que originó este ajuste. NULL = modalidad A (producción para stock)."),
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
                    table.ForeignKey(
                        name: "FK_inventoryAdjustment_productionOrder_idProductionOrder",
                        column: x => x.idProductionOrder,
                        principalTable: "productionOrder",
                        principalColumn: "idProductionOrder",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Documento de ajuste de inventario. El tipo (idInventoryAdjustmentType) define las cuentas contables para generar el asiento al confirmar. Estados: Borrador → Confirmado → Anulado.");

            migrationBuilder.CreateTable(
                name: "salesOrderAdvance",
                columns: table => new
                {
                    idSalesOrderAdvance = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del anticipo.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrder = table.Column<int>(type: "int", nullable: false, comment: "FK al pedido de venta al que corresponde este anticipo."),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false, comment: "FK al asiento contable que registra la recepción del anticipo."),
                    idProductionOrder = table.Column<int>(type: "int", nullable: true, comment: "FK informativa a la orden de producción en cuyo contexto se recibió el anticipo. No afecta la lógica financiera."),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto del anticipo en la moneda del pedido."),
                    dateAdvance = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha en que se recibió el anticipo."),
                    descriptionAdvance = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Nota opcional sobre el anticipo."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderAdvance", x => x.idSalesOrderAdvance);
                    table.ForeignKey(
                        name: "FK_salesOrderAdvance_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderAdvance_productionOrder_idProductionOrder",
                        column: x => x.idProductionOrder,
                        principalTable: "productionOrder",
                        principalColumn: "idProductionOrder",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderAdvance_salesOrder_idSalesOrder",
                        column: x => x.idSalesOrder,
                        principalTable: "salesOrder",
                        principalColumn: "idSalesOrder",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Anticipo o depósito recibido de un cliente contra un pedido de venta. Se aplica como crédito al emitir la SalesInvoice final. IdProductionOrder es contexto informativo sobre cuándo/por qué se recibió el anticipo.");

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
                    versionNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "Número de versión de la receta. Se incrementa al actualizar. Cada modificación crea una nueva fila; la anterior queda IsActive=false."),
                    nameRecipe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre descriptivo de la receta (ej: Cahuita Salsa Caribeña 160ml v2)."),
                    quantityOutput = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad producida por corrida, expresada en la unidad base del producto output."),
                    descriptionRecipe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Instrucciones generales u observaciones del proceso productivo."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Solo recetas activas se usan en producción. Al actualizar una receta la versión anterior queda IsActive=false."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación de esta versión.")
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
                name: "bankMovementDocument",
                columns: table => new
                {
                    idBankMovementDocument = table.Column<int>(type: "int", nullable: false, comment: "Identificador único del documento de soporte")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idBankMovement = table.Column<int>(type: "int", nullable: false, comment: "FK al movimiento bancario al que pertenece el documento"),
                    idPurchaseInvoice = table.Column<int>(type: "int", nullable: true, comment: "FK opcional a la factura de compra vinculada a este documento de soporte"),
                    idSalesInvoice = table.Column<int>(type: "int", nullable: true, comment: "FK opcional a la factura de venta vinculada a este documento de soporte"),
                    typeDocument = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Tipo de documento: 'FacturaCompra', 'FacturaVenta', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'"),
                    numberDocument = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true, comment: "Número o referencia del documento (factura, cheque, etc.)"),
                    dateDocument = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del documento de soporte"),
                    amountDocument = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto del documento de soporte"),
                    descriptionDocument = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Descripción adicional del documento")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bankMovementDocument", x => x.idBankMovementDocument);
                    table.CheckConstraint("CK_bankMovementDocument_typeDocument", "typeDocument IN ('FacturaCompra', 'FacturaVenta', 'Recibo', 'Transferencia', 'Cheque', 'Otro')");
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
                    table.ForeignKey(
                        name: "FK_bankMovementDocument_salesInvoice_idSalesInvoice",
                        column: x => x.idSalesInvoice,
                        principalTable: "salesInvoice",
                        principalColumn: "idSalesInvoice",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Documentos de soporte vinculados a un movimiento bancario");

            migrationBuilder.CreateTable(
                name: "salesInvoiceEntry",
                columns: table => new
                {
                    idSalesInvoiceEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoice = table.Column<int>(type: "int", nullable: false),
                    idAccountingEntry = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceEntry", x => x.idSalesInvoiceEntry);
                    table.ForeignKey(
                        name: "FK_salesInvoiceEntry_accountingEntry_idAccountingEntry",
                        column: x => x.idAccountingEntry,
                        principalTable: "accountingEntry",
                        principalColumn: "idAccountingEntry",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceEntry_salesInvoice_idSalesInvoice",
                        column: x => x.idSalesInvoice,
                        principalTable: "salesInvoice",
                        principalColumn: "idSalesInvoice",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla pivot N:M salesInvoice ↔ accountingEntry.");

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
                    quantityReserved = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 0m, comment: "Stock reservado por SalesOrderLineFulfillment de tipo Stock pendientes de confirmar. Se incrementa al asignar un fulfillment y se decrementa al confirmar o eliminar el fulfillment. QuantityAvailableNet = QuantityAvailable - QuantityReserved."),
                    statusLot = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Disponible", comment: "Estado de calidad del lote: Disponible | Cuarentena | Bloqueado | Vencido. Solo los lotes Disponibles son seleccionables en FEFO."),
                    sourceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, comment: "Origen del lote: Compra | Producción | Ajuste."),
                    idPurchaseInvoice = table.Column<int>(type: "int", nullable: true, comment: "FK a la factura de compra que originó este lote. Poblado si sourceType = 'Compra'."),
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: true, comment: "FK al ajuste de inventario que originó este lote. Poblado si sourceType = 'Ajuste' o 'Producción' (V1)."),
                    idWarehouse = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "FK al almacén donde se encuentra este lote. Por defecto el almacén Principal (id=1)."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryLot", x => x.idInventoryLot);
                    table.CheckConstraint("CK_inventoryLot_quantityReserved", "quantityReserved >= 0");
                    table.CheckConstraint("CK_inventoryLot_sourceType", "sourceType IN ('Compra', 'Producción', 'Ajuste')");
                    table.CheckConstraint("CK_inventoryLot_statusLot", "statusLot IN ('Disponible', 'Cuarentena', 'Bloqueado', 'Vencido')");
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
                    table.ForeignKey(
                        name: "FK_inventoryLot_warehouse_idWarehouse",
                        column: x => x.idWarehouse,
                        principalTable: "warehouse",
                        principalColumn: "idWarehouse",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Registro de stock de inventario por producto y lote. Es la unidad mínima de trazabilidad. quantityAvailable nunca se edita directamente: solo se modifica al confirmar purchaseInvoice, salesInvoice o inventoryAdjustment. quantityReserved se incrementa al asignar un SalesOrderLineFulfillment tipo Stock y se decrementa al confirmar o eliminar el fulfillment.");

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
                name: "productionSnapshot",
                columns: table => new
                {
                    idProductionSnapshot = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del snapshot de producción.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: false, comment: "FK 1:1 al ajuste de inventario de tipo PRODUCCION."),
                    idProductRecipe = table.Column<int>(type: "int", nullable: false, comment: "FK a la receta vigente al momento de confirmar la producción."),
                    quantityCalculated = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad teórica del producto final según la receta (ProductRecipe.QuantityOutput al confirmar)."),
                    quantityReal = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad real producida físicamente en esta corrida."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha y hora UTC en que se creó el snapshot.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productionSnapshot", x => x.idProductionSnapshot);
                    table.ForeignKey(
                        name: "FK_productionSnapshot_inventoryAdjustment_idInventoryAdjustment",
                        column: x => x.idInventoryAdjustment,
                        principalTable: "inventoryAdjustment",
                        principalColumn: "idInventoryAdjustment",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionSnapshot_productRecipe_idProductRecipe",
                        column: x => x.idProductRecipe,
                        principalTable: "productRecipe",
                        principalColumn: "idProductRecipe",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Copia de la receta usada al confirmar un ajuste de producción. Registra la cantidad calculada (teórica) y la real producida para permitir ajustar recetas a lo largo del tiempo.");

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
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de presentación dentro del grupo."),
                    idProductRecipe = table.Column<int>(type: "int", nullable: true, comment: "FK opcional a la receta que se usa para producir este option item (ej: receta de masa delgada).")
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
                    table.ForeignKey(
                        name: "FK_productOptionItem_productRecipe_idProductRecipe",
                        column: x => x.idProductRecipe,
                        principalTable: "productRecipe",
                        principalColumn: "idProductRecipe",
                        onDelete: ReferentialAction.Restrict);
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
                name: "priceListItem",
                columns: table => new
                {
                    idPriceListItem = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del ítem.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idPriceList = table.Column<int>(type: "int", nullable: false, comment: "FK a la lista de precios a la que pertenece este ítem."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto."),
                    idProductUnit = table.Column<int>(type: "int", nullable: false, comment: "FK a la presentación (unidad de venta) del producto."),
                    unitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Precio unitario del producto en esta presentación y lista."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Indica si el ítem está activo."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_priceListItem", x => x.idPriceListItem);
                    table.ForeignKey(
                        name: "FK_priceListItem_priceList_idPriceList",
                        column: x => x.idPriceList,
                        principalTable: "priceList",
                        principalColumn: "idPriceList",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_priceListItem_productUnit_idProductUnit",
                        column: x => x.idProductUnit,
                        principalTable: "productUnit",
                        principalColumn: "idProductUnit",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_priceListItem_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Ítem de lista de precios: precio unitario por producto y presentación (ProductUnit) dentro de una lista.");

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

            migrationBuilder.CreateTable(
                name: "inventoryAdjustmentLine",
                columns: table => new
                {
                    idInventoryAdjustmentLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idInventoryAdjustment = table.Column<int>(type: "int", nullable: false, comment: "FK al ajuste de inventario cabecera. Cascade delete."),
                    idInventoryLot = table.Column<int>(type: "int", nullable: true, comment: "FK al lote de inventario a ajustar. Exclusivo con idProduct."),
                    idProduct = table.Column<int>(type: "int", nullable: true, comment: "FK al producto para ajuste de costo promedio global. Exclusivo con idInventoryLot. Al confirmar: escala el unitCost de todos sus lotes proporcionalmente para que el costo promedio ponderado = unitCostNew."),
                    quantityDelta = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Delta en unidad base: positivo = entrada, negativo = salida, cero = ajuste de costo puro. Siempre 0 para líneas por producto."),
                    unitCostNew = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true, comment: "Costo unitario nuevo (ajuste por lote) o costo promedio objetivo (ajuste por producto). Requerido si quantityDelta > 0 o si se usa idProduct."),
                    descriptionLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Detalle por línea: insumo consumido, merma, motivo del ajuste, etc.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventoryAdjustmentLine", x => x.idInventoryAdjustmentLine);
                    table.CheckConstraint("CK_inventoryAdjustmentLine_productLevel", "idProduct IS NULL OR (quantityDelta = 0 AND unitCostNew IS NOT NULL)");
                    table.CheckConstraint("CK_inventoryAdjustmentLine_target", "(idInventoryLot IS NOT NULL AND idProduct IS NULL) OR (idInventoryLot IS NULL AND idProduct IS NOT NULL)");
                    table.CheckConstraint("CK_inventoryAdjustmentLine_unitCostNew", "idInventoryLot IS NULL OR quantityDelta <= 0 OR unitCostNew IS NOT NULL");
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
                    table.ForeignKey(
                        name: "FK_inventoryAdjustmentLine_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Líneas del ajuste de inventario. Cada línea referencia un lote (idInventoryLot) o un producto (idProduct), nunca ambos. quantityDelta positivo = entrada, negativo = salida, cero = ajuste de costo puro. Ajuste por lote: unitCostNew requerido si quantityDelta > 0. Ajuste por producto (idProduct): quantityDelta siempre 0 y unitCostNew = costo promedio objetivo; ajusta todos los lotes del producto proporcionalmente.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLine",
                columns: table => new
                {
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoice = table.Column<int>(type: "int", nullable: false),
                    isNonProductLine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "true = flete/servicio/gasto sin stock; false = producto. Cuando false y sin receta activa ni combo, idInventoryLot es obligatorio."),
                    idProduct = table.Column<int>(type: "int", nullable: true),
                    idUnit = table.Column<int>(type: "int", nullable: true),
                    idInventoryLot = table.Column<int>(type: "int", nullable: true),
                    idProductRecipe = table.Column<int>(type: "int", nullable: true, comment: "Snapshot FK de la receta usada al confirmar (explosión BOM). NULL si el producto no tiene receta activa o es combo."),
                    descriptionLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false, comment: "Descripción del producto o servicio de la línea."),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad en la unidad de venta seleccionada."),
                    quantityBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true, comment: "Cantidad en unidad base, calculada al confirmar: Quantity × ConversionFactor."),
                    unitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Precio unitario de venta en la presentación seleccionada."),
                    unitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true, comment: "Costo unitario snapshot del lote en el momento de confirmar (para calcular COGS)."),
                    taxPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "Porcentaje de impuesto aplicado a esta línea."),
                    totalLineAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Total de la línea incluyendo impuesto.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLine", x => x.idSalesInvoiceLine);
                    table.CheckConstraint("CK_salesInvoiceLine_lot_required", "isNonProductLine = 1 OR idInventoryLot IS NOT NULL OR idProductRecipe IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_productRecipe_idProductRecipe",
                        column: x => x.idProductRecipe,
                        principalTable: "productRecipe",
                        principalColumn: "idProductRecipe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_salesInvoice_idSalesInvoice",
                        column: x => x.idSalesInvoice,
                        principalTable: "salesInvoice",
                        principalColumn: "idSalesInvoice",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLine_unitOfMeasure_idUnit",
                        column: x => x.idUnit,
                        principalTable: "unitOfMeasure",
                        principalColumn: "idUnit",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Línea de la factura de venta. IsNonProductLine=false + producto sin receta ni combo: IdInventoryLot obligatorio (lote directo). IsNonProductLine=false + producto con receta activa: BOM explosion en ConfirmAsync (BomDetails). IsNonProductLine=false + combo: explosión de slots en ConfirmAsync (BomDetails). IsNonProductLine=true: flete/servicio/gasto, sin movimiento de inventario.");

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

            migrationBuilder.CreateTable(
                name: "productComboSlotPresetOption",
                columns: table => new
                {
                    idProductComboSlotPresetOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "FK al slot del combo al que pertenece esta opción preset."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción preseleccionado (debe pertenecer al producto del slot).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productComboSlotPresetOption", x => x.idProductComboSlotPresetOption);
                    table.ForeignKey(
                        name: "FK_productComboSlotPresetOption_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productComboSlotPresetOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Opciones preseleccionadas en el catálogo para un slot de combo. El cliente las ve bloqueadas (no editables).");

            migrationBuilder.CreateTable(
                name: "productOptionItemAvailability",
                columns: table => new
                {
                    idProductOptionItemAvailability = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idRestrictedItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem restringido."),
                    idEnablingItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem que habilita al restringido.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productOptionItemAvailability", x => x.idProductOptionItemAvailability);
                    table.ForeignKey(
                        name: "FK_productOptionItemAvailability_productOptionItem_idEnablingItem",
                        column: x => x.idEnablingItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productOptionItemAvailability_productOptionItem_idRestrictedItem",
                        column: x => x.idRestrictedItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Reglas de disponibilidad condicional entre items de opción. El item restringido (idRestrictedItem) solo está disponible cuando al menos uno de sus ítems habilitadores (idEnablingItem) está seleccionado en el pedido.");

            migrationBuilder.CreateTable(
                name: "productionSnapshotLine",
                columns: table => new
                {
                    idProductionSnapshotLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductionSnapshot = table.Column<int>(type: "int", nullable: false, comment: "FK al snapshot de producción cabecera."),
                    idProductRecipeLine = table.Column<int>(type: "int", nullable: true, comment: "FK a la línea de receta de origen. NULL si es un insumo extra no previsto en la receta."),
                    idProductInput = table.Column<int>(type: "int", nullable: false, comment: "Snapshot del producto insumo, desacoplado de la línea de receta para sobrevivir cambios futuros en la misma."),
                    quantityCalculated = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad teórica: ProductRecipeLine.QuantityInput × (QuantityReal / QuantityCalculated de la cabecera). 0 para insumos extra."),
                    quantityReal = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad real usada por el operador en esta corrida."),
                    sortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Orden de visualización, copiado de ProductRecipeLine.SortOrder.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productionSnapshotLine", x => x.idProductionSnapshotLine);
                    table.ForeignKey(
                        name: "FK_productionSnapshotLine_productRecipeLine_idProductRecipeLine",
                        column: x => x.idProductRecipeLine,
                        principalTable: "productRecipeLine",
                        principalColumn: "idProductRecipeLine",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_productionSnapshotLine_product_idProductInput",
                        column: x => x.idProductInput,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionSnapshotLine_productionSnapshot_idProductionSnapshot",
                        column: x => x.idProductionSnapshot,
                        principalTable: "productionSnapshot",
                        principalColumn: "idProductionSnapshot",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Línea del snapshot de producción. Una fila por insumo, con cantidad teórica calculada (según receta) y cantidad real usada. idProductRecipeLine es NULL cuando el operador agregó un insumo extra no previsto en la receta.");

            migrationBuilder.CreateTable(
                name: "salesOrderLine",
                columns: table => new
                {
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrder = table.Column<int>(type: "int", nullable: false, comment: "FK al pedido de venta cabecera."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto solicitado."),
                    idProductUnit = table.Column<int>(type: "int", nullable: false, comment: "FK a la presentación (unidad de venta) en que se pide el producto."),
                    idPriceListItem = table.Column<int>(type: "int", nullable: true, comment: "FK al ítem de lista de precios del que se tomó el precio. NULL si se ingresó manualmente."),
                    quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad en la unidad de venta solicitada."),
                    quantityBase = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad en unidad base, calculada × ConversionFactor al confirmar."),
                    unitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Snapshot del precio unitario al crear el pedido. No cambia aunque la lista de precios se actualice."),
                    taxPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "Porcentaje de impuesto al momento del pedido (ej: 13.00)."),
                    totalLineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total de la línea (Quantity × UnitPrice × (1 + TaxPercent / 100))."),
                    descriptionLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Descripción o nota adicional de la línea.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLine", x => x.idSalesOrderLine);
                    table.ForeignKey(
                        name: "FK_salesOrderLine_priceListItem_idPriceListItem",
                        column: x => x.idPriceListItem,
                        principalTable: "priceListItem",
                        principalColumn: "idPriceListItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLine_productUnit_idProductUnit",
                        column: x => x.idProductUnit,
                        principalTable: "productUnit",
                        principalColumn: "idProductUnit",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLine_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLine_salesOrder_idSalesOrder",
                        column: x => x.idSalesOrder,
                        principalTable: "salesOrder",
                        principalColumn: "idSalesOrder",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Línea del pedido de venta. UnitPrice es snapshot del precio de la lista vigente al crear el pedido.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineBomDetail",
                columns: table => new
                {
                    idSalesInvoiceLineBomDetail = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental del detalle BOM.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea de factura de venta que originó este movimiento."),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: true, comment: "FK nullable al slot del combo. NULL si la línea no es un combo."),
                    idProductRecipeLine = table.Column<int>(type: "int", nullable: true, comment: "FK nullable a la línea de receta. NULL si es reventa directa de slot o insumo extra."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Snapshot del insumo o producto de slot descontado al confirmar."),
                    idInventoryLot = table.Column<int>(type: "int", nullable: false, comment: "Lote específico del que se descontó el stock (FEFO auto-asignado)."),
                    quantityConsumed = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, comment: "Cantidad descontada en unidad base del insumo/producto."),
                    unitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Snapshot del costo unitario del lote al momento de confirmar la factura.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineBomDetail", x => x.idSalesInvoiceLineBomDetail);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_productRecipeLine_idProductRecipeLine",
                        column: x => x.idProductRecipeLine,
                        principalTable: "productRecipeLine",
                        principalColumn: "idProductRecipeLine",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineBomDetail_salesInvoiceLine_idSalesInvoiceLine",
                        column: x => x.idSalesInvoiceLine,
                        principalTable: "salesInvoiceLine",
                        principalColumn: "idSalesInvoiceLine",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Detalle de movimiento de inventario generado al confirmar una SalesInvoiceLine mediante explosión BOM (receta activa — Opción 2B) o por slot de combo (Opción 3A). Una línea puede originar N registros: uno por insumo de receta o por producto de slot. IdProductRecipeLine = NULL indica reventa directa de slot o insumo extra no previsto en receta.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineComboSlotSelection",
                columns: table => new
                {
                    idSalesInvoiceLineComboSlotSelection = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea de la factura (combo)."),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "FK al slot del combo."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Producto elegido en el slot (snapshot al momento de facturar)."),
                    idInventoryLot = table.Column<int>(type: "int", nullable: true, comment: "Lote de producto terminado pre-asignado desde producción (nullable — slot sin receta lo omite).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineComboSlotSelection", x => x.idSalesInvoiceLineComboSlotSelection);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineComboSlotSelection_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineComboSlotSelection_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineComboSlotSelection_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineComboSlotSelection_salesInvoiceLine_idSalesInvoiceLine",
                        column: x => x.idSalesInvoiceLine,
                        principalTable: "salesInvoiceLine",
                        principalColumn: "idSalesInvoiceLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Snapshot inmutable de la selección por slot al generar la factura de venta.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineEntry",
                columns: table => new
                {
                    idSalesInvoiceLineEntry = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false),
                    idAccountingEntryLine = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineEntry", x => x.idSalesInvoiceLineEntry);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineEntry_accountingEntryLine_idAccountingEntryLine",
                        column: x => x.idAccountingEntryLine,
                        principalTable: "accountingEntryLine",
                        principalColumn: "idAccountingEntryLine",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineEntry_salesInvoiceLine_idSalesInvoiceLine",
                        column: x => x.idSalesInvoiceLine,
                        principalTable: "salesInvoiceLine",
                        principalColumn: "idSalesInvoiceLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabla pivot N:M salesInvoiceLine ↔ accountingEntryLine.");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineOption",
                columns: table => new
                {
                    idSalesInvoiceLineOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea de la factura."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este option item aplicado a la línea.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineOption", x => x.idSalesInvoiceLineOption);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineOption_salesInvoiceLine_idSalesInvoiceLine",
                        column: x => x.idSalesInvoiceLine,
                        principalTable: "salesInvoiceLine",
                        principalColumn: "idSalesInvoiceLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opciones configurables copiadas desde el pedido a la factura de venta.");

            migrationBuilder.CreateTable(
                name: "productionOrderLine",
                columns: table => new
                {
                    idProductionOrderLine = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental de la línea.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idProductionOrder = table.Column<int>(type: "int", nullable: false, comment: "FK a la orden de producción cabecera."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "FK al producto final a producir en esta línea."),
                    idProductUnit = table.Column<int>(type: "int", nullable: false, comment: "FK a la unidad de producción del producto."),
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: true, comment: "FK opcional a la línea del pedido de venta que originó esta línea de producción."),
                    quantityRequired = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad total comprometida en unidad base que se debe producir para cubrir el pedido."),
                    quantityProduced = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m, comment: "Acumulado producido. Se incrementa cada vez que se confirma un InventoryAdjustment vinculado a esta orden."),
                    descriptionLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Nota adicional opcional de la línea.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productionOrderLine", x => x.idProductionOrderLine);
                    table.ForeignKey(
                        name: "FK_productionOrderLine_productUnit_idProductUnit",
                        column: x => x.idProductUnit,
                        principalTable: "productUnit",
                        principalColumn: "idProductUnit",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionOrderLine_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productionOrderLine_productionOrder_idProductionOrder",
                        column: x => x.idProductionOrder,
                        principalTable: "productionOrder",
                        principalColumn: "idProductionOrder",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productionOrderLine_salesOrderLine_idSalesOrderLine",
                        column: x => x.idSalesOrderLine,
                        principalTable: "salesOrderLine",
                        principalColumn: "idSalesOrderLine",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Línea de orden de producción. Registra qué producto producir, cuánto se requiere (QuantityRequired) y cuánto se ha producido acumulado (QuantityProduced). Vinculada opcionalmente a la línea del pedido de origen para calcular margen por pedido.");

            migrationBuilder.CreateTable(
                name: "salesOrderLineComboSlotSelection",
                columns: table => new
                {
                    idSalesOrderLineComboSlotSelection = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea del pedido (combo)."),
                    idProductComboSlot = table.Column<int>(type: "int", nullable: false, comment: "FK al slot del combo configurado."),
                    idProduct = table.Column<int>(type: "int", nullable: false, comment: "Producto elegido por el cliente en este slot.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLineComboSlotSelection", x => x.idSalesOrderLineComboSlotSelection);
                    table.ForeignKey(
                        name: "FK_salesOrderLineComboSlotSelection_productComboSlot_idProductComboSlot",
                        column: x => x.idProductComboSlot,
                        principalTable: "productComboSlot",
                        principalColumn: "idProductComboSlot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineComboSlotSelection_product_idProduct",
                        column: x => x.idProduct,
                        principalTable: "product",
                        principalColumn: "idProduct",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineComboSlotSelection_salesOrderLine_idSalesOrderLine",
                        column: x => x.idSalesOrderLine,
                        principalTable: "salesOrderLine",
                        principalColumn: "idSalesOrderLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Selección del cliente para cada slot del combo en una línea de pedido.");

            migrationBuilder.CreateTable(
                name: "salesOrderLineFulfillment",
                columns: table => new
                {
                    idSalesOrderLineFulfillment = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea del pedido que se está cumpliendo."),
                    fulfillmentType = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, comment: "Tipo: 'Stock' = se toma de un lote existente; 'Produccion' = se producirá contra esta línea."),
                    idInventoryLot = table.Column<int>(type: "int", nullable: true, comment: "FK al lote de inventario asignado cuando FulfillmentType = 'Stock'."),
                    idProductionOrder = table.Column<int>(type: "int", nullable: true, comment: "FK a la orden de producción cuando FulfillmentType = 'Produccion'."),
                    quantityBase = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "Cantidad en unidad base asignada a este fulfillment."),
                    unitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "Snapshot del costo unitario al confirmar la factura final."),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()", comment: "Fecha y hora UTC de creación.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLineFulfillment", x => x.idSalesOrderLineFulfillment);
                    table.CheckConstraint("CK_salesOrderLineFulfillment_lot_or_order", "(fulfillmentType = 'Stock' AND idInventoryLot IS NOT NULL AND idProductionOrder IS NULL) OR (fulfillmentType = 'Produccion' AND idProductionOrder IS NOT NULL)");
                    table.CheckConstraint("CK_salesOrderLineFulfillment_type", "fulfillmentType IN ('Stock', 'Produccion')");
                    table.ForeignKey(
                        name: "FK_salesOrderLineFulfillment_inventoryLot_idInventoryLot",
                        column: x => x.idInventoryLot,
                        principalTable: "inventoryLot",
                        principalColumn: "idInventoryLot",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineFulfillment_productionOrder_idProductionOrder",
                        column: x => x.idProductionOrder,
                        principalTable: "productionOrder",
                        principalColumn: "idProductionOrder",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineFulfillment_salesOrderLine_idSalesOrderLine",
                        column: x => x.idSalesOrderLine,
                        principalTable: "salesOrderLine",
                        principalColumn: "idSalesOrderLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Detalle de cómo se cumple cada línea del pedido: con stock existente (FulfillmentType='Stock' → IdInventoryLot) o con producción planificada (FulfillmentType='Produccion' → IdProductionOrder). Una línea puede tener múltiples registros para mezclar stock y producción.");

            migrationBuilder.CreateTable(
                name: "salesOrderLineOption",
                columns: table => new
                {
                    idSalesOrderLineOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrderLine = table.Column<int>(type: "int", nullable: false, comment: "FK a la línea del pedido."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción seleccionado."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este option item aplicado a la línea (por defecto 1).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLineOption", x => x.idSalesOrderLineOption);
                    table.ForeignKey(
                        name: "FK_salesOrderLineOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineOption_salesOrderLine_idSalesOrderLine",
                        column: x => x.idSalesOrderLine,
                        principalTable: "salesOrderLine",
                        principalColumn: "idSalesOrderLine",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opciones configurables seleccionadas en una línea de pedido (ej: masa delgada, extra queso).");

            migrationBuilder.CreateTable(
                name: "salesInvoiceLineSlotOption",
                columns: table => new
                {
                    idSalesInvoiceLineSlotOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesInvoiceLineComboSlotSelection = table.Column<int>(type: "int", nullable: false, comment: "FK a la selección de slot de la línea de factura."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción del slot."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este option item aplicado al slot."),
                    isPreset = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "true = opción copiada automáticamente del preset del slot; false = elegida libremente por el cliente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesInvoiceLineSlotOption", x => x.idSalesInvoiceLineSlotOption);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineSlotOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesInvoiceLineSlotOption_salesInvoiceLineComboSlotSelection_idSalesInvoiceLineComboSlotSelection",
                        column: x => x.idSalesInvoiceLineComboSlotSelection,
                        principalTable: "salesInvoiceLineComboSlotSelection",
                        principalColumn: "idSalesInvoiceLineComboSlotSelection",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opciones del slot incluidas en la factura (snapshot al copiar desde el pedido).");

            migrationBuilder.CreateTable(
                name: "salesOrderLineSlotOption",
                columns: table => new
                {
                    idSalesOrderLineSlotOption = table.Column<int>(type: "int", nullable: false, comment: "Identificador único autoincremental.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idSalesOrderLineComboSlotSelection = table.Column<int>(type: "int", nullable: false, comment: "FK a la selección de slot de la línea del pedido."),
                    idProductOptionItem = table.Column<int>(type: "int", nullable: false, comment: "FK al ítem de opción elegido dentro del slot."),
                    quantity = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false, defaultValue: 1m, comment: "Cantidad de este option item aplicado al slot (por defecto 1)."),
                    isPreset = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "true = opción copiada automáticamente del preset del slot; false = elegida libremente por el cliente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesOrderLineSlotOption", x => x.idSalesOrderLineSlotOption);
                    table.ForeignKey(
                        name: "FK_salesOrderLineSlotOption_productOptionItem_idProductOptionItem",
                        column: x => x.idProductOptionItem,
                        principalTable: "productOptionItem",
                        principalColumn: "idProductOptionItem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_salesOrderLineSlotOption_salesOrderLineComboSlotSelection_idSalesOrderLineComboSlotSelection",
                        column: x => x.idSalesOrderLineComboSlotSelection,
                        principalTable: "salesOrderLineComboSlotSelection",
                        principalColumn: "idSalesOrderLineComboSlotSelection",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opciones elegidas dentro de cada selección de slot (incluye presets copiados y opciones libres del cliente).");

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
                    { 3, "BNCR", true, "Banco Nacional de Costa Rica" },
                    { 4, "COOPEALIANZA", true, "Coopealianza" },
                    { 5, "DAVIVIENDA", true, "Davivienda" }
                });

            migrationBuilder.InsertData(
                table: "bankStatementTemplate",
                columns: new[] { "idBankStatementTemplate", "bankName", "codeTemplate", "columnMappings", "dateFormat", "isActive", "keywordRules", "nameTemplate", "notes", "timeFormat" },
                values: new object[,]
                {
                    { 1, "Banco de Costa Rica", "BCR-HTML-XLS-V1", "{\"accountingDate\":0,\"transactionDate\":1,\"transactionTime\":2,\"documentNumber\":3,\"description\":4,\"debitAmount\":5,\"creditAmount\":6,\"balance\":7,\"skipHeaderRows\":1}", "dd/MM/yyyy", true, "[\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\n                                                                        \"idBankMovementType\":1,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"DEP EFECTIVO\",\"DEPOSITO EFECTIVO\",\"DEPOSITO EN CAJA\"],\n                                                                        \"idBankMovementType\":2,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"INTERNET DTR SINPE\",\"DTR SINPE\",\"SINPE CR\",\"TRANSF CREDIT\",\"CREDITO SINPE\",\"SINPE MOVIL CR\",\"ABONO SINPE\",\"RECIBO SINPE\"],\n                                                                        \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"COMPRAS EN COMERCIOS\",\"COMPRA EN COMERCIO\",\"COMPRAS COMERC\",\"COMPRA COMERC\",\"DB AH TELEF\",\"MOVISTAR\",\"KOLBI\",\"PG AH TIEMPO AIRE TD\"],\n                                                                        \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"RETIRO ATM\",\"RETIRO CAJERO\",\"RETIRO EFECTIVO\",\"CAJERO AUTOMATICO\"],\n                                                                        \"idBankMovementType\":5,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO TC\",\"PAGO TARJETA\",\"TRJ CRED\",\"PAGO TARJETA CREDITO\",\"PAGO TRJ\",\"PAGO TARJETAS\",\"TRANSFERENC BANCOBCR\"],\n                                                                        \"idBankMovementType\":6,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO PREST\",\"CUOTA PREST\",\"PAGO PRESTAMO\",\"CUOTA PRESTAMO\"],\n                                                                        \"idBankMovementType\":7,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"SINPE MOVIL OTRA ENT\",\"OTRA ENT\",\"TRANSF DEB\",\"SINPE DEB\",\"DEB SINPE\",\"SINPE MOVIL DEB\",\"DEBITO SINPE\",\"TRANSFERENCIA SINPE DEB\",\"CARGO SINPE\",\"MONEDERO SINPE MOVIL\"],\n                                                                        \"idBankMovementType\":8,\"matchMode\":\"Any\"}\n]", "BCR – Movimientos de Cuenta (HTML-XLS)", "Archivo exportado como .xls desde el portal BCR. El contenido real es HTML con una tabla id='t1'. Aplica para cuentas de ahorros y cuentas corrientes en colones y dólares.", "HH:mm:ss" },
                    { 2, "BAC Credomatic", "BAC-TXT-V1", "{}", "dd/MM/yyyy", true, "[\n  {\"keywords\":[\"SU PAGO RECIBIDO GRACIAS\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"UBER\",\"DLC*UBER\",\"DLC*LYFT\",\"BOLT\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"APPLE.COM\",\"NETFLIX.COM\",\"GITHUB\",\"SPOTIFY\",\"YOUTUBE\",\"AMAZON\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"WALMART\",\"MAXIPALI\",\"MXM \",\"SUPER SALON\",\"AUTOMERCADO\",\"PALI \"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"IVA -\"],\n                                            \"idBankMovementType\":4,\"matchMode\":\"Any\"}\n]", "BAC Credomatic – Estado de Cuenta Tarjeta (TXT)", "Archivo .txt pipe-delimitado exportado desde el portal BAC. Aplica para estados de cuenta de tarjetas de crédito (AMEX, Visa, Mastercard). La columna Local contiene montos en CRC y Dollars en USD; se usa el no-cero.", null },
                    { 3, "Banco Nacional de Costa Rica", "BNCR-CSV-V1", "{}", "dd/MM/yyyy", true, "[\n  {\"keywords\":[\"SALARIO\",\"ITQS\",\"IT QUEST\",\"NOMINA\",\"PLANILLA\"],\n                                            \"idBankMovementType\":1,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"INTERESES GANADOS\"],\n                                            \"idBankMovementType\":2,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"TRANSFERENCIA SINPE\",\"SINPE MOVIL\",\"PAGO TARJETA BAC\",\"PAGOTARJETABAC\",\"SEMANA MAXIPAL\",\"PAGO SERVICIO PROFESIONAL\"],\n                                            \"idBankMovementType\":3,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"RETIRO ATM\",\"RETIRO CAJERO\",\"RETIRO EFECTIVO\"],\n                                            \"idBankMovementType\":5,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO TARJET\",\"PAGO TC\",\"TARJETA CRED\"],\n                                            \"idBankMovementType\":6,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"PAGO PREST\",\"CUOTA PREST\",\"PAGO PRESTAMO\",\"CUOTA PRESTAMO\"],\n                                            \"idBankMovementType\":7,\"matchMode\":\"Any\"},\n  {\"keywords\":[\"SINPE MOVIL DEB\",\"DEB SINPE\",\"CARGO SINPE\",\"TRANSF DEB\"],\n                                            \"idBankMovementType\":8,\"matchMode\":\"Any\"}\n]", "BNCR – Movimientos de Cuenta (CSV)", "Archivo .csv punto-y-coma exportado desde BN en línea. Codificación Latin-1/Windows-1252. Aplica para cuentas de ahorros en colones y dólares.", null }
                });

            migrationBuilder.InsertData(
                table: "company",
                columns: new[] { "idCompany", "codeCompany", "nameCompany" },
                values: new object[,]
                {
                    { 1, "FBS", "Familia Baltodano Soto" },
                    { 2, "CDSRL", "Corporacion los diablitos SRL" }
                });

            migrationBuilder.InsertData(
                table: "contact",
                columns: new[] { "idContact", "codeContact", "name" },
                values: new object[] { 1, "SIN_PRO_CLI", "Sin proveedor / Cliente" });

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
                columns: new[] { "idProductType", "descriptionProductType", "nameProductType", "trackInventory" },
                values: new object[,]
                {
                    { 1, "Insumos o materiales adquiridos para ser utilizados en el proceso productivo. No se venden directamente.", "Materia Prima", true },
                    { 2, "Productos que han iniciado su proceso de fabricación pero aún no están terminados.", "Producto en Proceso", true },
                    { 3, "Productos que han completado el proceso productivo y están listos para la venta.", "Producto Terminado", true },
                    { 4, "Productos adquiridos listos para la venta sin transformación productiva.", "Reventa", true }
                });

            migrationBuilder.InsertData(
                table: "productType",
                columns: new[] { "idProductType", "descriptionProductType", "nameProductType" },
                values: new object[] { 5, "Servicios, mano de obra o conceptos sin stock físico. No generan movimientos de inventario.", "Servicios" });

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
                table: "unitType",
                columns: new[] { "idUnitType", "nameUnitType" },
                values: new object[,]
                {
                    { 1, "Unidad" },
                    { 2, "Volumen" },
                    { 3, "Masa" },
                    { 4, "Longitud" }
                });

            migrationBuilder.InsertData(
                table: "user",
                columns: new[] { "idUser", "codeUser", "createAt", "emailUser", "nameUser", "phoneUser" },
                values: new object[] { 1, "S", new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "ezekiell1988@hotmail.com", "Ezequiel Baltodano Cubillo", "50683681485" });

            migrationBuilder.InsertData(
                table: "warehouse",
                columns: new[] { "idWarehouse", "isActive", "isDefault", "nameWarehouse" },
                values: new object[] { 1, true, true, "Principal" });

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
                    { 112, false, "5.14", 5, true, 2, "Ajustes de Inventario", "Gasto" },
                    { 116, false, "4.5", 4, true, 2, "Ingresos por Ventas", "Ingreso" },
                    { 118, false, "5.15", 5, true, 2, "Costo de Ventas", "Gasto" }
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
                table: "contactContactType",
                columns: new[] { "idContactContactType", "idContact", "idContactType" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 1, 2 }
                });

            migrationBuilder.InsertData(
                table: "unitOfMeasure",
                columns: new[] { "idUnit", "codeUnit", "idUnitType", "nameUnit" },
                values: new object[,]
                {
                    { 1, "U", 1, "Unidad" },
                    { 2, "M3", 2, "Metro Cúbico" },
                    { 3, "KG", 3, "Kilogramo" },
                    { 4, "M", 4, "Metro" },
                    { 5, "GR", 3, "Gramo" },
                    { 6, "ML", 2, "Mililitro" },
                    { 7, "LTR", 2, "Litro" }
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
                    { 33, false, "1.1.03", 7, true, 3, "Banco Nacional de Costa Rica (BNCR)", "Activo" },
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
                    { 113, false, "5.14.01", 112, true, 3, "Faltantes de Inventario (Merma)", "Gasto" },
                    { 114, true, "5.14.02", 112, true, 3, "Sobrantes de Inventario", "Gasto" },
                    { 115, true, "5.14.03", 112, true, 3, "Costos de Producción", "Gasto" },
                    { 117, true, "4.5.01", 116, true, 3, "Ingresos por Ventas — Mercadería", "Ingreso" },
                    { 119, true, "5.15.01", 118, true, 3, "Costo de Ventas — Mercadería", "Gasto" },
                    { 120, false, "1.1.08", 7, true, 3, "Cuentas por Cobrar", "Activo" },
                    { 123, false, "1.1.09", 7, true, 3, "IVA Acreditable", "Activo" },
                    { 126, false, "2.1.04", 9, true, 3, "IVA por Pagar", "Pasivo" }
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
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[,]
                {
                    { 1, null, "REV-COCA-001", null, 4, 1, "Coca-Cola 355ml", null, null, null },
                    { 2, null, "MP-CHILE-001", null, 1, 3, "Chile Seco", null, null, null },
                    { 3, null, "MP-VINAGRE-001", null, 1, 7, "Vinagre Blanco", null, null, null },
                    { 4, null, "MP-SAL-001", null, 1, 3, "Sal", null, null, null },
                    { 5, null, "MP-FRASCO-001", null, 1, 1, "Frasco 250ml", null, null, null },
                    { 6, null, "PT-CHILE-EMB-001", null, 3, 1, "Chile Embotellado Marca X", null, null, null },
                    { 7, null, "MP-PAN-HD-001", null, 1, 1, "Pan de Hot Dog", null, null, null },
                    { 8, null, "MP-SALCHICHA-001", null, 1, 1, "Salchicha", null, null, null },
                    { 9, null, "MP-MOSTAZA-001", null, 1, 6, "Mostaza", null, null, null },
                    { 10, null, "MP-CATSUP-001", null, 1, 6, "Catsup", null, null, null },
                    { 11, null, "PT-HOT-DOG-001", null, 3, 1, "Hot Dog", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "isVariantParent", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[] { 12, null, "CAMISA-OXF-000", null, 4, 1, true, "Camisa Oxford", null, null, null });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[,]
                {
                    { 18, null, "MP-HARINA-001", null, 1, 3, "Harina de Trigo", null, null, null },
                    { 19, null, "MP-AGUA-001", null, 1, 7, "Agua", null, null, null },
                    { 20, null, "MP-LEVADURA-001", null, 1, 5, "Levadura", null, null, null },
                    { 21, null, "MP-ACEITE-001", null, 1, 6, "Aceite de Oliva", null, null, null },
                    { 22, null, "MP-SALSA-TOM-001", null, 1, 6, "Salsa de Tomate", null, null, null },
                    { 23, null, "MP-MOZZ-001", null, 1, 3, "Queso Mozzarella", null, null, null },
                    { 24, null, "MP-PEPPERONI-001", null, 1, 3, "Pepperoni", null, null, null },
                    { 25, null, "MP-PINA-001", null, 1, 3, "Piña en Rodajas", null, null, null },
                    { 26, null, "MP-JAMON-001", null, 1, 3, "Jamón", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "hasOptions", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[] { 27, null, "PT-PIZZA-001", true, null, 3, 1, "Pizza", null, null, null });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[,]
                {
                    { 28, null, "REV-SPRITE-001", null, 4, 1, "Sprite 355ml", null, null, null },
                    { 29, null, "REV-AGUA-BOT-001", null, 4, 1, "Agua Pura Botella 500ml", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "isCombo", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[] { 30, null, "COMBO-2PIZ-BEB", null, 4, 1, true, "Combo 2 Pizzas + Bebida", null, null, null });

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "idAccount", "allowsMovements", "codeAccount", "idAccountParent", "isActive", "levelAccount", "nameAccount", "typeAccount" },
                values: new object[,]
                {
                    { 25, true, "1.1.01.01", 24, true, 4, "BCR - Cta. 07015202001294229652 - Soto Arce Karen Tatiana", "Activo" },
                    { 27, true, "1.1.02.01", 26, true, 4, "BAC - Cta. CR73010200009497305680 - Baltodano Cubillo Ezequiel", "Activo" },
                    { 29, true, "2.1.01.01", 28, true, 4, "BAC - AMEX  CR64010202312918989651 (₡) - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 30, true, "2.1.01.03", 28, true, 4, "BAC - MCARD CR69010202510369031047 (₡) - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 31, true, "2.1.01.05", 28, true, 4, "BAC - MCARD CR48010202514509181545 (₡) - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 32, true, "2.1.01.07", 28, true, 4, "BAC - VISA  ****-1593               (₡) - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 34, true, "1.1.03.01", 33, true, 4, "BNCR - Cta. CR86015100020019688637 (₡) - Baltodano Cubillo Ezequiel", "Activo" },
                    { 35, true, "1.1.03.02", 33, true, 4, "BNCR - Cta. CR06015107220020012339 ($) - Baltodano Cubillo Ezequiel", "Activo" },
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
                    { 111, true, "1.1.07.03", 108, true, 4, "Productos en Proceso", "Activo" },
                    { 121, true, "1.1.08.01", 120, true, 4, "Cuentas por Cobrar — Clientes CRC (₡)", "Activo" },
                    { 122, true, "1.1.08.02", 120, true, 4, "Cuentas por Cobrar — Clientes USD ($)", "Activo" },
                    { 124, true, "1.1.09.01", 123, true, 4, "IVA Acreditable CRC (₡)", "Activo" },
                    { 125, true, "1.1.09.02", 123, true, 4, "IVA Acreditable USD ($)", "Activo" },
                    { 127, true, "2.1.04.01", 126, true, 4, "IVA por Pagar CRC (₡)", "Pasivo" },
                    { 128, true, "2.1.04.02", 126, true, 4, "IVA por Pagar USD ($)", "Pasivo" },
                    { 129, true, "5.14.01.01", 113, true, 4, "Merma Normal", "Gasto" },
                    { 130, true, "5.14.01.02", 113, true, 4, "Merma Anormal", "Gasto" },
                    { 131, true, "2.1.01.02", 28, true, 4, "BAC - AMEX  CR13010202321157328803 ($) - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 132, true, "2.1.01.04", 28, true, 4, "BAC - MCARD CR17010202526537778556 ($) - Baltodano Cubillo Ezequiel", "Pasivo" },
                    { 133, true, "2.1.01.06", 28, true, 4, "BAC - MCARD CR18010202522447454214 ($) - Baltodano Cubillo Ezequiel", "Pasivo" }
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
                table: "product",
                columns: new[] { "idProduct", "classificationAbc", "codeProduct", "idProductParent", "idProductType", "idUnit", "nameProduct", "reorderPoint", "reorderQuantity", "safetyStock" },
                values: new object[,]
                {
                    { 13, null, "CAMISA-OXF-S-AZ", 12, 4, 1, "Camisa Oxford Talla S Azul", null, null, null },
                    { 14, null, "CAMISA-OXF-M-AZ", 12, 4, 1, "Camisa Oxford Talla M Azul", null, null, null },
                    { 15, null, "CAMISA-OXF-L-AZ", 12, 4, 1, "Camisa Oxford Talla L Azul", null, null, null },
                    { 16, null, "CAMISA-OXF-S-RJ", 12, 4, 1, "Camisa Oxford Talla S Rojo", null, null, null },
                    { 17, null, "CAMISA-OXF-M-RJ", 12, 4, 1, "Camisa Oxford Talla M Rojo", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "productAttribute",
                columns: new[] { "idProductAttribute", "idProduct", "nameAttribute", "sortOrder" },
                values: new object[,]
                {
                    { 1, 12, "Talla", 1 },
                    { 2, 12, "Color", 2 }
                });

            migrationBuilder.InsertData(
                table: "productComboSlot",
                columns: new[] { "idProductComboSlot", "idProductCombo", "isRequired", "nameSlot", "quantity", "sortOrder" },
                values: new object[,]
                {
                    { 1, 30, true, "Pizza #1", 1m, 1 },
                    { 2, 30, true, "Pizza #2", 1m, 2 },
                    { 3, 30, true, "Bebida", 1m, 3 }
                });

            migrationBuilder.InsertData(
                table: "productOptionGroup",
                columns: new[] { "idProductOptionGroup", "idProduct", "isRequired", "maxSelections", "minSelections", "nameGroup", "sortOrder" },
                values: new object[,]
                {
                    { 1, 27, true, 1, 1, "Elige tu tamaño", 1 },
                    { 2, 27, true, 1, 1, "Elige tu masa", 2 },
                    { 3, 27, true, 1, 1, "Elige tu sabor", 3 }
                });

            migrationBuilder.InsertData(
                table: "productOptionGroup",
                columns: new[] { "idProductOptionGroup", "idProduct", "maxSelections", "nameGroup", "sortOrder" },
                values: new object[] { 4, 27, 3, "Extras", 4 });

            migrationBuilder.InsertData(
                table: "productRecipe",
                columns: new[] { "idProductRecipe", "createdAt", "descriptionRecipe", "idProductOutput", "isActive", "nameRecipe", "quantityOutput", "versionNumber" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 6, true, "Receta Chile Embotellado", 1m, 1 },
                    { 2, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 11, true, "Receta Hot Dog", 1m, 1 },
                    { 3, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, true, "Base Pizza", 1m, 1 }
                });

            migrationBuilder.InsertData(
                table: "productRecipe",
                columns: new[] { "idProductRecipe", "createdAt", "descriptionRecipe", "idProductOutput", "nameRecipe", "quantityOutput", "versionNumber" },
                values: new object[,]
                {
                    { 4, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, "Opción Sabor: Pepperoni", 1m, 2 },
                    { 5, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, "Opción Sabor: Hawaiian", 1m, 3 },
                    { 6, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, "Opción Tamaño: Grande", 1m, 4 },
                    { 7, new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 27, "Opción Extra: Doble Queso", 1m, 5 }
                });

            migrationBuilder.InsertData(
                table: "attributeValue",
                columns: new[] { "idAttributeValue", "idProductAttribute", "nameValue", "sortOrder" },
                values: new object[,]
                {
                    { 1, 1, "S", 1 },
                    { 2, 1, "M", 2 },
                    { 3, 1, "L", 3 },
                    { 4, 2, "Azul", 1 },
                    { 5, 2, "Rojo", 2 }
                });

            migrationBuilder.InsertData(
                table: "bankAccount",
                columns: new[] { "idBankAccount", "accountHolder", "accountNumber", "codeBankAccount", "idAccount", "idBank", "idCurrency", "isActive" },
                values: new object[,]
                {
                    { 1, "Soto Arce Karen Tatiana", "07015202001294229652", "BCR-AHO-001", 25, 1, 1, true },
                    { 2, "Baltodano Cubillo Ezequiel", "CR73010200009497305680", "BAC-AHO-001", 27, 2, 1, true },
                    { 3, "Baltodano Cubillo Ezequiel", "CR64010202312918989651", "BAC-CC-AMEX-8052-CRC", 29, 2, 1, true },
                    { 4, "Baltodano Cubillo Ezequiel", "CR69010202510369031047", "BAC-CC-MC-6515-CRC", 30, 2, 1, true },
                    { 5, "Baltodano Cubillo Ezequiel", "CR48010202514509181545", "BAC-CC-MC-8608-CRC", 31, 2, 1, true },
                    { 6, "Baltodano Cubillo Ezequiel", "****-1593", "BAC-CC-VISA-1593-CRC", 32, 2, 1, true },
                    { 7, "Baltodano Cubillo Ezequiel", "CR86015100020019688637", "BNCR-AHO-CRC-001", 34, 3, 1, true },
                    { 8, "Baltodano Cubillo Ezequiel", "CR06015107220020012339", "BNCR-AHO-USD-001", 35, 3, 2, true },
                    { 9, "Baltodano Cubillo Ezequiel", "CR54081300210008440287", "COOPEAL-AHO-001", 39, 4, 1, true },
                    { 10, "Baltodano Cubillo Ezequiel", "CR02081300010008440263", "COOPEAL-PAT-001", 37, 4, 1, true },
                    { 11, "Baltodano Cubillo Ezequiel", "CR98010401446613244113", "DAVIV-AHO-001", 55, 5, 1, true },
                    { 12, "Baltodano Cubillo Ezequiel", "CR13010202321157328803", "BAC-CC-AMEX-8052-USD", 131, 2, 2, true },
                    { 13, "Baltodano Cubillo Ezequiel", "CR17010202526537778556", "BAC-CC-MC-6515-USD", 132, 2, 2, true },
                    { 14, "Baltodano Cubillo Ezequiel", "CR18010202522447454214", "BAC-CC-MC-8608-USD", 133, 2, 2, true }
                });

            migrationBuilder.InsertData(
                table: "bankMovementType",
                columns: new[] { "idBankMovementType", "codeBankMovementType", "idAccountCounterpart", "isActive", "movementSign", "nameBankMovementType" },
                values: new object[,]
                {
                    { 1, "SAL", 44, true, "Abono", "Depósito de Salario" },
                    { 7, "PAGO-PREST", 42, true, "Cargo", "Pago de Préstamo" },
                    { 8, "TRANSF-ENV", 34, true, "Cargo", "Transferencia Enviada" },
                    { 9, "COBRO-CRC", 121, true, "Abono", "Cobro de Venta a Crédito (₡)" },
                    { 10, "COBRO-USD", 122, true, "Abono", "Cobro de Venta a Crédito ($)" }
                });

            migrationBuilder.InsertData(
                table: "inventoryAdjustmentType",
                columns: new[] { "idInventoryAdjustmentType", "codeInventoryAdjustmentType", "idAccountCounterpartEntry", "idAccountCounterpartExit", "idAccountInventoryDefault", "isActive", "nameInventoryAdjustmentType" },
                values: new object[,]
                {
                    { 1, "CONTEO", 114, 130, 109, true, "Conteo Físico" },
                    { 2, "PRODUCCION", 115, 115, 111, true, "Producción" },
                    { 3, "AJUSTE_COSTO", 114, 130, 109, true, "Ajuste de Costo" },
                    { 4, "REGALIA", null, 130, 109, true, "Regalía" }
                });

            migrationBuilder.InsertData(
                table: "productComboSlotProduct",
                columns: new[] { "idProductComboSlotProduct", "idProduct", "idProductComboSlot", "sortOrder" },
                values: new object[,]
                {
                    { 1, 27, 1, 1 },
                    { 2, 27, 2, 1 },
                    { 3, 1, 3, 1 },
                    { 4, 28, 3, 2 },
                    { 5, 29, 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "isDefault", "nameItem", "sortOrder" },
                values: new object[] { 1, 1, null, true, "Mediana", 1 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "nameItem", "priceDelta", "sortOrder" },
                values: new object[] { 2, 1, 6, "Grande", 2.00m, 2 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "isDefault", "nameItem", "sortOrder" },
                values: new object[] { 3, 2, null, true, "Clásica", 1 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "nameItem", "sortOrder" },
                values: new object[] { 4, 2, null, "Delgada", 2 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "isDefault", "nameItem", "sortOrder" },
                values: new object[] { 5, 3, 4, true, "Pepperoni", 1 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "nameItem", "sortOrder" },
                values: new object[] { 6, 3, 5, "Hawaiian", 2 });

            migrationBuilder.InsertData(
                table: "productOptionItem",
                columns: new[] { "idProductOptionItem", "idProductOptionGroup", "idProductRecipe", "nameItem", "priceDelta", "sortOrder" },
                values: new object[] { 7, 4, 7, "Doble Queso", 0.75m, 1 });

            migrationBuilder.InsertData(
                table: "productRecipeLine",
                columns: new[] { "idProductRecipeLine", "idProductInput", "idProductRecipe", "quantityInput", "sortOrder" },
                values: new object[,]
                {
                    { 1, 2, 1, 0.2000m, 1 },
                    { 2, 3, 1, 0.0500m, 2 },
                    { 3, 4, 1, 0.0050m, 3 },
                    { 4, 5, 1, 1.0000m, 4 },
                    { 5, 7, 2, 1.0000m, 1 },
                    { 6, 8, 2, 1.0000m, 2 },
                    { 7, 9, 2, 15.0000m, 3 },
                    { 8, 10, 2, 20.0000m, 4 },
                    { 9, 18, 3, 0.4000m, 1 },
                    { 10, 19, 3, 0.2500m, 2 },
                    { 11, 20, 3, 5.0000m, 3 },
                    { 12, 21, 3, 30.0000m, 4 },
                    { 13, 22, 3, 100.000m, 5 },
                    { 14, 23, 3, 0.1500m, 6 },
                    { 15, 24, 4, 0.1000m, 1 },
                    { 16, 25, 5, 0.0800m, 1 },
                    { 17, 26, 5, 0.0800m, 2 },
                    { 18, 18, 6, 0.1500m, 1 },
                    { 19, 19, 6, 0.0800m, 2 },
                    { 20, 23, 7, 0.0500m, 1 }
                });

            migrationBuilder.InsertData(
                table: "purchaseInvoiceType",
                columns: new[] { "idPurchaseInvoiceType", "codePurchaseInvoiceType", "counterpartFromBankMovement", "idAccountCounterpartCRC", "idAccountCounterpartUSD", "idBankMovementType", "idDefaultExpenseAccount", "idDefaultInventoryAccount", "isActive", "namePurchaseInvoiceType" },
                values: new object[,]
                {
                    { 1, "EFECTIVO", false, 106, 107, null, 75, 109, true, "Pago en Efectivo" },
                    { 2, "DEBITO", true, null, null, 4, 75, 109, true, "Tarjeta de Débito / Transferencia" },
                    { 3, "TC", true, null, null, 6, 75, 109, true, "Tarjeta de Crédito" }
                });

            migrationBuilder.InsertData(
                table: "salesInvoiceType",
                columns: new[] { "idSalesInvoiceType", "codeSalesInvoiceType", "counterpartFromBankMovement", "idAccountCOGS", "idAccountCounterpartCRC", "idAccountCounterpartUSD", "idAccountInventory", "idAccountSalesRevenue", "idBankMovementType", "isActive", "nameSalesInvoiceType" },
                values: new object[,]
                {
                    { 1, "CONTADO_CRC", false, 119, 106, null, 109, 117, null, true, "Venta Contado CRC" },
                    { 2, "CONTADO_USD", false, 119, null, 107, 109, 117, null, true, "Venta Contado USD" }
                });

            migrationBuilder.InsertData(
                table: "productComboSlotPresetOption",
                columns: new[] { "idProductComboSlotPresetOption", "idProductComboSlot", "idProductOptionItem" },
                values: new object[,]
                {
                    { 1, 1, 2 },
                    { 2, 2, 2 }
                });

            migrationBuilder.InsertData(
                table: "productVariantAttribute",
                columns: new[] { "idProductVariantAttribute", "idAttributeValue", "idProduct" },
                values: new object[,]
                {
                    { 1, 1, 13 },
                    { 2, 4, 13 },
                    { 3, 2, 14 },
                    { 4, 4, 14 },
                    { 5, 3, 15 },
                    { 6, 4, 15 },
                    { 7, 1, 16 },
                    { 8, 5, 16 },
                    { 9, 2, 17 },
                    { 10, 5, 17 }
                });

            migrationBuilder.InsertData(
                table: "salesInvoiceType",
                columns: new[] { "idSalesInvoiceType", "codeSalesInvoiceType", "counterpartFromBankMovement", "idAccountCOGS", "idAccountCounterpartCRC", "idAccountCounterpartUSD", "idAccountInventory", "idAccountSalesRevenue", "idBankMovementType", "isActive", "nameSalesInvoiceType" },
                values: new object[,]
                {
                    { 3, "CREDITO_CRC", true, 119, null, null, 109, 117, 9, true, "Venta a Crédito CRC (₡)" },
                    { 4, "CREDITO_USD", true, 119, null, null, 109, 117, 10, true, "Venta a Crédito USD ($)" }
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
                name: "UQ_attributeValue_idProductAttribute_nameValue",
                table: "attributeValue",
                columns: new[] { "idProductAttribute", "nameValue" },
                unique: true);

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
                name: "IX_bankMovementDocument_idSalesInvoice",
                table: "bankMovementDocument",
                column: "idSalesInvoice",
                filter: "[idSalesInvoice] IS NOT NULL");

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
                name: "IX_inventoryAdjustment_idProductionOrder",
                table: "inventoryAdjustment",
                column: "idProductionOrder",
                filter: "[idProductionOrder] IS NOT NULL");

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
                name: "IX_inventoryAdjustmentLine_idProduct",
                table: "inventoryAdjustmentLine",
                column: "idProduct",
                filter: "[idProduct] IS NOT NULL");

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
                column: "idInventoryAdjustment",
                filter: "[idInventoryAdjustment] IS NOT NULL");

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
                name: "IX_inventoryLot_idWarehouse",
                table: "inventoryLot",
                column: "idWarehouse");

            migrationBuilder.CreateIndex(
                name: "IX_priceListItem_idProduct",
                table: "priceListItem",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_priceListItem_idProductUnit",
                table: "priceListItem",
                column: "idProductUnit");

            migrationBuilder.CreateIndex(
                name: "UQ_priceListItem_idPriceList_idProduct_idProductUnit",
                table: "priceListItem",
                columns: new[] { "idPriceList", "idProduct", "idProductUnit" },
                unique: true);

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
                name: "UQ_productAttribute_idProduct_nameAttribute",
                table: "productAttribute",
                columns: new[] { "idProduct", "nameAttribute" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productComboSlot_idProductCombo",
                table: "productComboSlot",
                column: "idProductCombo");

            migrationBuilder.CreateIndex(
                name: "IX_productComboSlotPresetOption_idProductOptionItem",
                table: "productComboSlotPresetOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "UQ_productComboSlotPresetOption_slot_item",
                table: "productComboSlotPresetOption",
                columns: new[] { "idProductComboSlot", "idProductOptionItem" },
                unique: true);

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
                name: "IX_productionOrder_idFiscalPeriod",
                table: "productionOrder",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrder_idSalesOrder",
                table: "productionOrder",
                column: "idSalesOrder",
                filter: "[idSalesOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrder_idWarehouse",
                table: "productionOrder",
                column: "idWarehouse",
                filter: "[idWarehouse] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_productionOrder_numberProductionOrder",
                table: "productionOrder",
                column: "numberProductionOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productionOrderLine_idProduct",
                table: "productionOrderLine",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrderLine_idProductionOrder",
                table: "productionOrderLine",
                column: "idProductionOrder");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrderLine_idProductUnit",
                table: "productionOrderLine",
                column: "idProductUnit");

            migrationBuilder.CreateIndex(
                name: "IX_productionOrderLine_idSalesOrderLine",
                table: "productionOrderLine",
                column: "idSalesOrderLine",
                filter: "[idSalesOrderLine] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_productionSnapshot_idProductRecipe",
                table: "productionSnapshot",
                column: "idProductRecipe");

            migrationBuilder.CreateIndex(
                name: "UQ_productionSnapshot_idInventoryAdjustment",
                table: "productionSnapshot",
                column: "idInventoryAdjustment",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productionSnapshotLine_idProductInput",
                table: "productionSnapshotLine",
                column: "idProductInput");

            migrationBuilder.CreateIndex(
                name: "IX_productionSnapshotLine_idProductionSnapshot",
                table: "productionSnapshotLine",
                column: "idProductionSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_productionSnapshotLine_idProductRecipeLine",
                table: "productionSnapshotLine",
                column: "idProductRecipeLine",
                filter: "[idProductRecipeLine] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_productOptionGroup_idProduct",
                table: "productOptionGroup",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_productOptionItem_idProductOptionGroup",
                table: "productOptionItem",
                column: "idProductOptionGroup");

            migrationBuilder.CreateIndex(
                name: "IX_productOptionItem_idProductRecipe",
                table: "productOptionItem",
                column: "idProductRecipe");

            migrationBuilder.CreateIndex(
                name: "IX_productOptionItemAvailability_idEnablingItem",
                table: "productOptionItemAvailability",
                column: "idEnablingItem");

            migrationBuilder.CreateIndex(
                name: "UQ_productOptionItemAvailability_idRestrictedItem_idEnablingItem",
                table: "productOptionItemAvailability",
                columns: new[] { "idRestrictedItem", "idEnablingItem" },
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
                name: "UQ_productRecipe_idProductOutput_versionNumber",
                table: "productRecipe",
                columns: new[] { "idProductOutput", "versionNumber" },
                unique: true);

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
                name: "UQ_productUnit_idProduct_isBase",
                table: "productUnit",
                column: "idProduct",
                unique: true,
                filter: "[isBase] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_productVariantAttribute_idAttributeValue",
                table: "productVariantAttribute",
                column: "idAttributeValue");

            migrationBuilder.CreateIndex(
                name: "UQ_productVariantAttribute_idProduct_idAttributeValue",
                table: "productVariantAttribute",
                columns: new[] { "idProduct", "idAttributeValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idBankAccount",
                table: "purchaseInvoice",
                column: "idBankAccount");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseInvoice_idContact",
                table: "purchaseInvoice",
                column: "idContact");

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
                name: "IX_purchaseInvoice_idWarehouse",
                table: "purchaseInvoice",
                column: "idWarehouse",
                filter: "[idWarehouse] IS NOT NULL");

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
                name: "IX_purchaseInvoiceType_idDefaultInventoryAccount",
                table: "purchaseInvoiceType",
                column: "idDefaultInventoryAccount",
                filter: "[idDefaultInventoryAccount] IS NOT NULL");

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
                name: "IX_salesInvoice_idBankAccount",
                table: "salesInvoice",
                column: "idBankAccount");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idContact",
                table: "salesInvoice",
                column: "idContact",
                filter: "[idContact] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idCurrency",
                table: "salesInvoice",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idFiscalPeriod",
                table: "salesInvoice",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idSalesInvoiceType",
                table: "salesInvoice",
                column: "idSalesInvoiceType");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoice_idSalesOrder",
                table: "salesInvoice",
                column: "idSalesOrder",
                filter: "[idSalesOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoice_numberInvoice",
                table: "salesInvoice",
                column: "numberInvoice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceEntry_idAccountingEntry",
                table: "salesInvoiceEntry",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceEntry_idSalesInvoice_idAccountingEntry",
                table: "salesInvoiceEntry",
                columns: new[] { "idSalesInvoice", "idAccountingEntry" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idInventoryLot",
                table: "salesInvoiceLine",
                column: "idInventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idProduct",
                table: "salesInvoiceLine",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idProductRecipe",
                table: "salesInvoiceLine",
                column: "idProductRecipe");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idSalesInvoice",
                table: "salesInvoiceLine",
                column: "idSalesInvoice");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLine_idUnit",
                table: "salesInvoiceLine",
                column: "idUnit");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idInventoryLot",
                table: "salesInvoiceLineBomDetail",
                column: "idInventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idProduct",
                table: "salesInvoiceLineBomDetail",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idProductComboSlot",
                table: "salesInvoiceLineBomDetail",
                column: "idProductComboSlot",
                filter: "[idProductComboSlot] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idProductRecipeLine",
                table: "salesInvoiceLineBomDetail",
                column: "idProductRecipeLine",
                filter: "[idProductRecipeLine] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineBomDetail_idSalesInvoiceLine",
                table: "salesInvoiceLineBomDetail",
                column: "idSalesInvoiceLine");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineComboSlotSelection_idInventoryLot",
                table: "salesInvoiceLineComboSlotSelection",
                column: "idInventoryLot");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineComboSlotSelection_idProduct",
                table: "salesInvoiceLineComboSlotSelection",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineComboSlotSelection_idProductComboSlot",
                table: "salesInvoiceLineComboSlotSelection",
                column: "idProductComboSlot");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceLineComboSlotSelection_line_slot",
                table: "salesInvoiceLineComboSlotSelection",
                columns: new[] { "idSalesInvoiceLine", "idProductComboSlot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineEntry_idAccountingEntryLine",
                table: "salesInvoiceLineEntry",
                column: "idAccountingEntryLine");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceLineEntry_idSalesInvoiceLine_idAccountingEntryLine",
                table: "salesInvoiceLineEntry",
                columns: new[] { "idSalesInvoiceLine", "idAccountingEntryLine" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineOption_idProductOptionItem",
                table: "salesInvoiceLineOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineOption_idSalesInvoiceLine",
                table: "salesInvoiceLineOption",
                column: "idSalesInvoiceLine");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceLineSlotOption_idProductOptionItem",
                table: "salesInvoiceLineSlotOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceLineSlotOption_selection_item",
                table: "salesInvoiceLineSlotOption",
                columns: new[] { "idSalesInvoiceLineComboSlotSelection", "idProductOptionItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountCOGS",
                table: "salesInvoiceType",
                column: "idAccountCOGS");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountCounterpartCRC",
                table: "salesInvoiceType",
                column: "idAccountCounterpartCRC",
                filter: "[idAccountCounterpartCRC] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountCounterpartUSD",
                table: "salesInvoiceType",
                column: "idAccountCounterpartUSD",
                filter: "[idAccountCounterpartUSD] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountInventory",
                table: "salesInvoiceType",
                column: "idAccountInventory");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idAccountSalesRevenue",
                table: "salesInvoiceType",
                column: "idAccountSalesRevenue");

            migrationBuilder.CreateIndex(
                name: "IX_salesInvoiceType_idBankMovementType",
                table: "salesInvoiceType",
                column: "idBankMovementType",
                filter: "[idBankMovementType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_salesInvoiceType_codeSalesInvoiceType",
                table: "salesInvoiceType",
                column: "codeSalesInvoiceType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesOrder_idContact",
                table: "salesOrder",
                column: "idContact");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrder_idCurrency",
                table: "salesOrder",
                column: "idCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrder_idFiscalPeriod",
                table: "salesOrder",
                column: "idFiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrder_idPriceList",
                table: "salesOrder",
                column: "idPriceList",
                filter: "[idPriceList] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_salesOrder_numberOrder",
                table: "salesOrder",
                column: "numberOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderAdvance_idAccountingEntry",
                table: "salesOrderAdvance",
                column: "idAccountingEntry");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderAdvance_idProductionOrder",
                table: "salesOrderAdvance",
                column: "idProductionOrder",
                filter: "[idProductionOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderAdvance_idSalesOrder",
                table: "salesOrderAdvance",
                column: "idSalesOrder");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLine_idPriceListItem",
                table: "salesOrderLine",
                column: "idPriceListItem",
                filter: "[idPriceListItem] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLine_idProduct",
                table: "salesOrderLine",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLine_idProductUnit",
                table: "salesOrderLine",
                column: "idProductUnit");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLine_idSalesOrder",
                table: "salesOrderLine",
                column: "idSalesOrder");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineComboSlotSelection_idProduct",
                table: "salesOrderLineComboSlotSelection",
                column: "idProduct");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineComboSlotSelection_idProductComboSlot",
                table: "salesOrderLineComboSlotSelection",
                column: "idProductComboSlot");

            migrationBuilder.CreateIndex(
                name: "UQ_salesOrderLineComboSlotSelection_line_slot",
                table: "salesOrderLineComboSlotSelection",
                columns: new[] { "idSalesOrderLine", "idProductComboSlot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineFulfillment_idInventoryLot",
                table: "salesOrderLineFulfillment",
                column: "idInventoryLot",
                filter: "[idInventoryLot] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineFulfillment_idProductionOrder",
                table: "salesOrderLineFulfillment",
                column: "idProductionOrder",
                filter: "[idProductionOrder] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineFulfillment_idSalesOrderLine",
                table: "salesOrderLineFulfillment",
                column: "idSalesOrderLine");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineOption_idProductOptionItem",
                table: "salesOrderLineOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineOption_idSalesOrderLine",
                table: "salesOrderLineOption",
                column: "idSalesOrderLine");

            migrationBuilder.CreateIndex(
                name: "IX_salesOrderLineSlotOption_idProductOptionItem",
                table: "salesOrderLineSlotOption",
                column: "idProductOptionItem");

            migrationBuilder.CreateIndex(
                name: "UQ_salesOrderLineSlotOption_selection_item",
                table: "salesOrderLineSlotOption",
                columns: new[] { "idSalesOrderLineComboSlotSelection", "idProductOptionItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unitOfMeasure_idUnitType",
                table: "unitOfMeasure",
                column: "idUnitType");

            migrationBuilder.CreateIndex(
                name: "UQ_unitOfMeasure_codeUnit",
                table: "unitOfMeasure",
                column: "codeUnit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_unitType_nameUnitType",
                table: "unitType",
                column: "nameUnitType",
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

            migrationBuilder.CreateIndex(
                name: "UQ_warehouse_nameWarehouse",
                table: "warehouse",
                column: "nameWarehouse",
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
                name: "productComboSlotPresetOption");

            migrationBuilder.DropTable(
                name: "productComboSlotProduct");

            migrationBuilder.DropTable(
                name: "productionOrderLine");

            migrationBuilder.DropTable(
                name: "productionSnapshotLine");

            migrationBuilder.DropTable(
                name: "productOptionItemAvailability");

            migrationBuilder.DropTable(
                name: "productProductCategory");

            migrationBuilder.DropTable(
                name: "productVariantAttribute");

            migrationBuilder.DropTable(
                name: "purchaseInvoiceEntry");

            migrationBuilder.DropTable(
                name: "purchaseInvoiceLineEntry");

            migrationBuilder.DropTable(
                name: "salesInvoiceEntry");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineBomDetail");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineEntry");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineOption");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineSlotOption");

            migrationBuilder.DropTable(
                name: "salesOrderAdvance");

            migrationBuilder.DropTable(
                name: "salesOrderLineFulfillment");

            migrationBuilder.DropTable(
                name: "salesOrderLineOption");

            migrationBuilder.DropTable(
                name: "salesOrderLineSlotOption");

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
                name: "productionSnapshot");

            migrationBuilder.DropTable(
                name: "productCategory");

            migrationBuilder.DropTable(
                name: "attributeValue");

            migrationBuilder.DropTable(
                name: "purchaseInvoiceLine");

            migrationBuilder.DropTable(
                name: "productRecipeLine");

            migrationBuilder.DropTable(
                name: "accountingEntryLine");

            migrationBuilder.DropTable(
                name: "salesInvoiceLineComboSlotSelection");

            migrationBuilder.DropTable(
                name: "productOptionItem");

            migrationBuilder.DropTable(
                name: "salesOrderLineComboSlotSelection");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "bankStatementTemplate");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "productAttribute");

            migrationBuilder.DropTable(
                name: "accountingEntry");

            migrationBuilder.DropTable(
                name: "costCenter");

            migrationBuilder.DropTable(
                name: "salesInvoiceLine");

            migrationBuilder.DropTable(
                name: "productOptionGroup");

            migrationBuilder.DropTable(
                name: "productComboSlot");

            migrationBuilder.DropTable(
                name: "salesOrderLine");

            migrationBuilder.DropTable(
                name: "inventoryLot");

            migrationBuilder.DropTable(
                name: "productRecipe");

            migrationBuilder.DropTable(
                name: "salesInvoice");

            migrationBuilder.DropTable(
                name: "priceListItem");

            migrationBuilder.DropTable(
                name: "inventoryAdjustment");

            migrationBuilder.DropTable(
                name: "purchaseInvoice");

            migrationBuilder.DropTable(
                name: "salesInvoiceType");

            migrationBuilder.DropTable(
                name: "productUnit");

            migrationBuilder.DropTable(
                name: "inventoryAdjustmentType");

            migrationBuilder.DropTable(
                name: "productionOrder");

            migrationBuilder.DropTable(
                name: "bankAccount");

            migrationBuilder.DropTable(
                name: "purchaseInvoiceType");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "salesOrder");

            migrationBuilder.DropTable(
                name: "warehouse");

            migrationBuilder.DropTable(
                name: "bank");

            migrationBuilder.DropTable(
                name: "bankMovementType");

            migrationBuilder.DropTable(
                name: "productType");

            migrationBuilder.DropTable(
                name: "unitOfMeasure");

            migrationBuilder.DropTable(
                name: "contact");

            migrationBuilder.DropTable(
                name: "currency");

            migrationBuilder.DropTable(
                name: "fiscalPeriod");

            migrationBuilder.DropTable(
                name: "priceList");

            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "unitType");
        }
    }
}
