using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankStatementTemplateConfiguration : IEntityTypeConfiguration<BankStatementTemplate>
{
    public void Configure(EntityTypeBuilder<BankStatementTemplate> builder)
    {
        builder.ToTable(t => t.HasComment("Plantillas de carga para extractos bancarios por entidad financiera"));

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(b => b.IdBankStatementTemplate);
        builder.Property(b => b.IdBankStatementTemplate)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único de la plantilla");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(b => b.CodeTemplate)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código único de la plantilla (p. ej. BCR-CHECKING-2024)");

        builder.Property(b => b.NameTemplate)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre descriptivo de la plantilla");

        builder.Property(b => b.BankName)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Nombre del banco emisor del extracto");

        builder.Property(b => b.ColumnMappings)
            .IsRequired()
            .HasComment("Mapeo de columnas en formato JSON con índices y nombres de campos del Excel");

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si la plantilla está activa para uso");

        // ── Campos opcionales ───────────────────────────────
        builder.Property(b => b.DateFormat)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasComment("Formato de fecha usado en el Excel (p. ej. dd/MM/yyyy)");

        builder.Property(b => b.TimeFormat)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasComment("Formato de hora usado en el Excel (p. ej. HH:mm)");

        builder.Property(b => b.Notes)
            .HasMaxLength(1000)
            .HasComment("Notas o instrucciones adicionales para el uso de la plantilla");

        builder.Property(b => b.KeywordRules)
            .HasComment("Reglas de palabras clave en formato JSON para auto-clasificar transacciones durante la importaci\u00f3n");

        // ── Índice único ─────────────────────────────────────
        builder.HasIndex(b => b.CodeTemplate)
            .IsUnique()
            .HasDatabaseName("UQ_bankStatementTemplate_codeTemplate");

        // ── Seed ──────────────────────────────────────────────────────────────
        // Plantilla BCR: el archivo .xls del portal BCR es HTML con tabla id="t1".
        // ColumnMappings usa los índices de BcrColumnMapping (0=FechaContable,
        // 1=FechaTrx, 2=Hora, 3=Documento, 4=Descripción, 5=Débito, 6=Crédito, 7=Saldo).
        //
        // KeywordRules — relacionadas con BankMovementType seed:
        //   1=SAL  Depósito de Salario    (Abono)
        //   2=DEP  Depósito en Efectivo   (Abono)
        //   3=TRANSF-REC Transferencia Recibida (Abono)
        //   5=RET  Retiro en Efectivo     (Cargo)
        //   6=PAGO-TC Pago Tarjeta Crédito (Cargo)
        //   7=PAGO-PREST Pago de Préstamo  (Cargo)
        //   8=TRANSF-ENV Transferencia Enviada (Cargo)
        //
        // BankAccount IdBankAccount=1 → BCR-AHO-001 (CR07015202001294229652, Soto Arce Karen Tatiana)
        // La asociación plantilla↔cuenta se realiza al crear el BankStatementImport.
        builder.HasData(new BankStatementTemplate
        {
            IdBankStatementTemplate = 1,
            CodeTemplate  = "BCR-HTML-XLS-V1",
            NameTemplate  = "BCR – Movimientos de Cuenta (HTML-XLS)",
            BankName      = "Banco de Costa Rica",
            DateFormat    = "dd/MM/yyyy",
            TimeFormat    = "HH:mm:ss",
            IsActive      = true,
            Notes         = "Archivo exportado como .xls desde el portal BCR. El contenido real es HTML con una tabla id='t1'. " +
                            "Aplica para cuentas de ahorros y cuentas corrientes en colones y dólares.",
            ColumnMappings = """{"accountingDate":0,"transactionDate":1,"transactionTime":2,"documentNumber":3,"description":4,"debitAmount":5,"creditAmount":6,"balance":7,"skipHeaderRows":1}""",
            KeywordRules   = """
[
  {"keywords":["SALARIO","ITQS","IT QUEST","NOMINA","PLANILLA"],
                                                                        "idBankMovementType":1,"matchMode":"Any"},
  {"keywords":["DEP EFECTIVO","DEPOSITO EFECTIVO","DEPOSITO EN CAJA"],
                                                                        "idBankMovementType":2,"matchMode":"Any"},
  {"keywords":["INTERNET DTR SINPE","DTR SINPE","SINPE CR","TRANSF CREDIT","CREDITO SINPE","SINPE MOVIL CR","ABONO SINPE","RECIBO SINPE"],
                                                                        "idBankMovementType":3,"matchMode":"Any"},
  {"keywords":["COMPRAS EN COMERCIOS","COMPRA EN COMERCIO","COMPRAS COMERC","COMPRA COMERC","DB AH TELEF","MOVISTAR","KOLBI","PG AH TIEMPO AIRE TD"],
                                                                        "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["RETIRO ATM","RETIRO CAJERO","RETIRO EFECTIVO","CAJERO AUTOMATICO"],
                                                                        "idBankMovementType":5,"matchMode":"Any"},
  {"keywords":["PAGO TC","PAGO TARJETA","TRJ CRED","PAGO TARJETA CREDITO","PAGO TRJ","PAGO TARJETAS","TRANSFERENC BANCOBCR"],
                                                                        "idBankMovementType":6,"matchMode":"Any"},
  {"keywords":["PAGO PREST","CUOTA PREST","PAGO PRESTAMO","CUOTA PRESTAMO"],
                                                                        "idBankMovementType":7,"matchMode":"Any"},
  {"keywords":["SINPE MOVIL OTRA ENT","OTRA ENT","TRANSF DEB","SINPE DEB","DEB SINPE","SINPE MOVIL DEB","DEBITO SINPE","TRANSFERENCIA SINPE DEB","CARGO SINPE","MONEDERO SINPE MOVIL"],
                                                                        "idBankMovementType":8,"matchMode":"Any"}
]
"""
        },

        // ── BAC Credomatic ─────────────────────────────────────────────────────────
        // Archivo TXT pipe-delimitado exportado desde el portal BAC.
        // Aplica para cuentas de tarjetas de crédito (IdBankAccount 3–6).
        // Formato de columnas: Date | Descripción | Local CRC | Dollars USD
        //   Montos positivos = cargo (DebitAmount)
        //   Montos negativos = pago recibido (CreditAmount)
        new BankStatementTemplate
        {
            IdBankStatementTemplate = 2,
            CodeTemplate  = "BAC-TXT-V1",
            NameTemplate  = "BAC Credomatic – Estado de Cuenta Tarjeta (TXT)",
            BankName      = "BAC Credomatic",
            DateFormat    = "dd/MM/yyyy",
            TimeFormat    = null,
            IsActive      = true,
            Notes         = "Archivo .txt pipe-delimitado exportado desde el portal BAC. " +
                            "Aplica para estados de cuenta de tarjetas de crédito (AMEX, Visa, Mastercard). " +
                            "La columna Local contiene montos en CRC y Dollars en USD; se usa el no-cero.",
            ColumnMappings = "{}",
            KeywordRules   = """
[
  {"keywords":["SU PAGO RECIBIDO GRACIAS"],
                                            "idBankMovementType":3,"matchMode":"Any"},
  {"keywords":["UBER","DLC*UBER","DLC*LYFT","BOLT"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["APPLE.COM","NETFLIX.COM","GITHUB","SPOTIFY","YOUTUBE","AMAZON"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["WALMART","MAXIPALI","MXM ","SUPER SALON","AUTOMERCADO","PALI "],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["IVA -"],
                                            "idBankMovementType":4,"matchMode":"Any"}
]
"""
        },

        // ── Banco Nacional de Costa Rica (BNCR) ────────────────────────────────────
        // Archivo CSV punto-y-coma exportado desde el portal BN en línea.
        // Aplica para cuentas de ahorros (IdBankAccount 7–8).
        // Formato de columnas: oficina ; fechaMovimiento ; numeroDocumento ; debito ; credito ; descripcion
        new BankStatementTemplate
        {
            IdBankStatementTemplate = 3,
            CodeTemplate  = "BNCR-CSV-V1",
            NameTemplate  = "BNCR – Movimientos de Cuenta (CSV)",
            BankName      = "Banco Nacional de Costa Rica",
            DateFormat    = "dd/MM/yyyy",
            TimeFormat    = null,
            IsActive      = true,
            Notes         = "Archivo .csv punto-y-coma exportado desde BN en línea. " +
                            "Codificación Latin-1/Windows-1252. " +
                            "Aplica para cuentas de ahorros en colones y dólares.",
            ColumnMappings = "{}",
            KeywordRules   = """
[
  {"keywords":["SALARIO","ITQS","IT QUEST","NOMINA","PLANILLA"],
                                            "idBankMovementType":1,"matchMode":"Any"},
  {"keywords":["INTERESES GANADOS"],
                                            "idBankMovementType":2,"matchMode":"Any"},
  {"keywords":["TRANSFERENCIA SINPE","SINPE MOVIL","PAGO TARJETA BAC","PAGOTARJETABAC","SEMANA MAXIPAL","PAGO SERVICIO PROFESIONAL"],
                                            "idBankMovementType":3,"matchMode":"Any"},
  {"keywords":["RETIRO ATM","RETIRO CAJERO","RETIRO EFECTIVO"],
                                            "idBankMovementType":5,"matchMode":"Any"},
  {"keywords":["PAGO TARJET","PAGO TC","TARJETA CRED"],
                                            "idBankMovementType":6,"matchMode":"Any"},
  {"keywords":["PAGO PREST","CUOTA PREST","PAGO PRESTAMO","CUOTA PRESTAMO"],
                                            "idBankMovementType":7,"matchMode":"Any"},
  {"keywords":["SINPE MOVIL DEB","DEB SINPE","CARGO SINPE","TRANSF DEB"],
                                            "idBankMovementType":8,"matchMode":"Any"}
]
"""
        },

        // ── BAC Credomatic – Tarjeta crédito CRC ───────────────────────────────────
        // Archivo TXT pipe-delimitado. Se usa SÓLO la columna Local (CRC).
        // Cuentas seed CRC: IdBankAccount 3 (AMEX), 4 (MC-6515), 5 (MC-8608), 6 (VISA-1593)
        new BankStatementTemplate
        {
            IdBankStatementTemplate = 4,
            CodeTemplate  = "BAC-TXT-CRC-V1",
            NameTemplate  = "BAC Credomatic – Tarjeta Crédito CRC (TXT)",
            BankName      = "BAC Credomatic",
            DateFormat    = "dd/MM/yyyy",
            TimeFormat    = null,
            IsActive      = true,
            Notes         = "Archivo .txt pipe-delimitado exportado desde el portal BAC. " +
                            "Sólo se procesa la columna Local (CRC). " +
                            "Usar para archivos *-CRC.txt de tarjetas de crédito en colones.",
            ColumnMappings = """{"currency":"CRC"}""",
            KeywordRules   = """
[
  {"keywords":["SU PAGO RECIBIDO GRACIAS"],
                                            "idBankMovementType":3,"matchMode":"Any"},
  {"keywords":["UBER","DLC*UBER","DLC*LYFT","BOLT"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["APPLE.COM","NETFLIX.COM","GITHUB","SPOTIFY","YOUTUBE","AMAZON"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["WALMART","MAXIPALI","MXM ","SUPER SALON","AUTOMERCADO","PALI ","SIMAN","ALMACENES"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["FARMACIA","DROGUERIA","CLINICA ","HOSPITAL","OPTICA ","LABORATORIO"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["FERRETERIA","DEPOSITO FERR","CONSTRUPLAZA"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["GOOGLE","MICROSOFT","2CO.COM","OPENAI","CHATGPT","DIGITALOCEAN","NEOTHEK"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["SEGURO PROTECCION","SEGURO DE VIDA","PRIMA SEGURO","INS "],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["IVA -"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["TRASLADO SALDO REVOLUTIVO","CUOTA:"],
                                            "idBankMovementType":7,"matchMode":"Any"}
]
"""
        },

        // ── BAC Credomatic – Tarjeta crédito USD ───────────────────────────────────
        // Archivo TXT pipe-delimitado. Se usa SÓLO la columna Dollars (USD).
        // Cuentas seed USD: IdBankAccount 12 (AMEX), 13 (MC-6515), 14 (MC-8608)
        new BankStatementTemplate
        {
            IdBankStatementTemplate = 5,
            CodeTemplate  = "BAC-TXT-USD-V1",
            NameTemplate  = "BAC Credomatic – Tarjeta Crédito USD (TXT)",
            BankName      = "BAC Credomatic",
            DateFormat    = "dd/MM/yyyy",
            TimeFormat    = null,
            IsActive      = true,
            Notes         = "Archivo .txt pipe-delimitado exportado desde el portal BAC. " +
                            "Sólo se procesa la columna Dollars (USD). " +
                            "Usar para archivos *-USD.txt de tarjetas de crédito en dólares.",
            ColumnMappings = """{"currency":"USD"}""",
            KeywordRules   = """
[
  {"keywords":["SU PAGO RECIBIDO GRACIAS"],
                                            "idBankMovementType":3,"matchMode":"Any"},
  {"keywords":["UBER","DLC*UBER","DLC*LYFT","BOLT"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["APPLE.COM","NETFLIX.COM","GITHUB","SPOTIFY","YOUTUBE","AMAZON","JETBRAINS","GOOGLE"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["GAMMA.APP","OPENAI","CHATGPT","MICROSOFT","DIGITALOCEAN","2CO.COM","NEOTHEK"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["ICON CC RETAIL","WALMART","AMAZON","SIMAN"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["SEGURO PROTECCION","SEGURO DE VIDA","PRIMA SEGURO"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["IVA -"],
                                            "idBankMovementType":4,"matchMode":"Any"},
  {"keywords":["TRASLADO SALDO REVOLUTIVO","CUOTA:"],
                                            "idBankMovementType":7,"matchMode":"Any"}
]
"""
        },

        // ── BAC Credomatic – Cuenta de ahorro/débito (XLS) ────────────────────────
        // Archivo .xls BIFF8 exportado desde el portal BAC para cuentas de ahorro.
        // IdBankAccount=2 → BAC-AHO-001 (CR73010200009497305680, CRC)
        // Columnas: 0=Fecha  1=Referencia  3=Código  4=Descripción  7=Débitos  8=Créditos  9=Balance
        new BankStatementTemplate
        {
            IdBankStatementTemplate = 6,
            CodeTemplate  = "BAC-XLS-V1",
            NameTemplate  = "BAC Credomatic – Cuenta de Ahorro/Débito (XLS)",
            BankName      = "BAC Credomatic",
            DateFormat    = "dd/MM/yyyy",
            TimeFormat    = null,
            IsActive      = true,
            Notes         = "Archivo .xls (BIFF8) exportado desde el portal BAC para cuentas de ahorro y débito. " +
                            "Columnas fijas: Fecha | Referencia | | Código | Descripción | | | Débitos | Créditos | Balance. " +
                            "Usar para cuentas de ahorro BAC (cuenta CR73... en CRC).",
            ColumnMappings = "{}",
            KeywordRules   = """
[
  {"keywords":["SALARIO","ITQS","IT QUEST","NOMINA","PLANILLA"],
                                            "idBankMovementType":1,"matchMode":"Any"},
  {"keywords":["DEP_ATM","TATMFULL","DEPOSITO ATM"],
                                            "idBankMovementType":2,"matchMode":"Any"},
  {"keywords":["TEF DE:","DTR SINPE","SINPE REC","ABONO SINPE","CREDITO SINPE"],
                                            "idBankMovementType":3,"matchMode":"Any"},
  {"keywords":["COOPEALIANZA","CAJA AHORRO"],
                                            "idBankMovementType":7,"matchMode":"Any"},
  {"keywords":["PAGO ","SINPE MOVIL PAGO_TARJETA"],
                                            "idBankMovementType":6,"matchMode":"Any"},
  {"keywords":["DTR:","RETIRO CAJERO","RETIRO ATM","RETIRO EFECTIVO"],
                                            "idBankMovementType":8,"matchMode":"Any"}
]
"""
        });
    }
}
