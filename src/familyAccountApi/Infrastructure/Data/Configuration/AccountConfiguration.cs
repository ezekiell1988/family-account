using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        // ── PK ───────────────────────────────────────────────
        builder.HasKey(a => a.IdAccount);
        builder.Property(a => a.IdAccount)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la cuenta contable.");

        // ── Comentario de tabla + check constraint ────────────
        builder.ToTable(t =>
        {
            t.HasComment("Catálogo de cuentas contables con jerarquía auto-referenciada. Permite registrar el plan de cuentas con padres e hijos. typeAccount: Activo, Pasivo, Capital, Ingreso, Gasto, Control.");
            t.HasCheckConstraint(
                "CK_account_typeAccount",
                "typeAccount IN ('Activo', 'Pasivo', 'Capital', 'Ingreso', 'Gasto', 'Control')");
        });

        // ── Campos ───────────────────────────────────────────
        builder.Property(a => a.CodeAccount)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código jerárquico único de la cuenta. Ej: '1', '1.1', '1.1.01'.");

        builder.Property(a => a.NameAccount)
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("Nombre descriptivo de la cuenta contable.");

        builder.Property(a => a.TypeAccount)
            .HasMaxLength(20)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Tipo contable: Activo | Pasivo | Capital | Ingreso | Gasto | Control.");

        builder.Property(a => a.LevelAccount)
            .IsRequired()
            .HasComment("Nivel jerárquico dentro del plan de cuentas. 1 = cuenta raíz.");

        builder.Property(a => a.IdAccountParent)
            .HasComment("FK a la cuenta padre. NULL indica que es una cuenta raíz.");

        builder.Property(a => a.AllowsMovements)
            .IsRequired()
            .HasComment("Indica si la cuenta acepta asientos contables directos (true) o es solo agrupadora (false).");

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasComment("Indica si la cuenta está activa y disponible para su uso.");

        // ── Índice único ──────────────────────────────────────
        builder.HasIndex(a => a.CodeAccount)
            .IsUnique()
            .HasDatabaseName("UQ_account_codeAccount");

        // ── FK auto-referenciada ──────────────────────────────
        // DeleteBehavior.Restrict porque SQL Server no permite Cascade en ciclos
        builder.HasOne(a => a.Parent)
            .WithMany(a => a.Children)
            .HasForeignKey(a => a.IdAccountParent)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Seed: plan contable básico (nivel 1 = raíces, nivel 2 = grupos) ─────
        builder.HasData(
            // ── Nivel 1: cuentas raíz ──────────────────────────────────────────
            new Account { IdAccount = 1, CodeAccount = "1", NameAccount = "Activo",  TypeAccount = "Activo",  LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 2, CodeAccount = "2", NameAccount = "Pasivo",  TypeAccount = "Pasivo",  LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 3, CodeAccount = "3", NameAccount = "Capital", TypeAccount = "Capital", LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 4, CodeAccount = "4", NameAccount = "Ingreso", TypeAccount = "Ingreso", LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 5, CodeAccount = "5", NameAccount = "Gasto",   TypeAccount = "Gasto",   LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 6, CodeAccount = "6", NameAccount = "Control", TypeAccount = "Control", LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },

            // ── Nivel 2: Activo ────────────────────────────────────────────────
            new Account { IdAccount = 7,  CodeAccount = "1.1", NameAccount = "Activo Corriente",       TypeAccount = "Activo",  LevelAccount = 2, IdAccountParent = 1,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 8,  CodeAccount = "1.2", NameAccount = "Activo No Corriente",    TypeAccount = "Activo",  LevelAccount = 2, IdAccountParent = 1,  AllowsMovements = false, IsActive = true },

            // ── Nivel 3: Bancos (hijos de Activo Corriente) ───────────────────
            new Account { IdAccount = 24, CodeAccount = "1.1.01", NameAccount = "Banco de Costa Rica (BCR)", TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 7,  AllowsMovements = false, IsActive = true },

            // ── Nivel 4: Cuentas bancarias específicas ────────────────────────
            new Account { IdAccount = 25, CodeAccount = "1.1.01.01", NameAccount = "BCR - Cta. 07015202001294229652 - Soto Arce Karen Tatiana", TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 24, AllowsMovements = true, IsActive = true },

            // ── Nivel 3: BAC Credomatic (hijos de Activo Corriente) ───────────
            new Account { IdAccount = 26, CodeAccount = "1.1.02",    NameAccount = "BAC Credomatic",                                                           TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 7,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 27, CodeAccount = "1.1.02.01", NameAccount = "BAC - Cta. CR73010200009497305680 - Baltodano Cubillo Ezequiel",            TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 26, AllowsMovements = true,  IsActive = true },

            // ── Nivel 3: Banco Nacional de Costa Rica (BN) ───────────────────
            new Account { IdAccount = 33, CodeAccount = "1.1.03",    NameAccount = "Banco Nacional de Costa Rica (BN)",                                        TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 7,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 34, CodeAccount = "1.1.03.01", NameAccount = "BN - Cta. CR86015100020019688637 (₡) - Baltodano Cubillo Ezequiel",        TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 33, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 35, CodeAccount = "1.1.03.02", NameAccount = "BN - Cta. CR06015107220020012339 ($) - Baltodano Cubillo Ezequiel",        TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 33, AllowsMovements = true,  IsActive = true },

            // ── Nivel 3: Coopealianza (hijos de Activo Corriente) ────────────
            new Account { IdAccount = 38, CodeAccount = "1.1.04",    NameAccount = "Coopealianza",                                                                            TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 7,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 39, CodeAccount = "1.1.04.01", NameAccount = "Coopealianza - Cta. CR54081300210008440287 (₡) - Baltodano Cubillo Ezequiel",             TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 38, AllowsMovements = true,  IsActive = true },

            // ── Nivel 3/4: Davivienda (hijos de Activo Corriente) ────────────
            new Account { IdAccount = 54, CodeAccount = "1.1.05",    NameAccount = "Davivienda",                                                                                    TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 7,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 55, CodeAccount = "1.1.05.01", NameAccount = "Davivienda - AHO CR98010401446613244113 (₡) - Baltodano Cubillo Ezequiel [Nómina ITQS]",         TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 54, AllowsMovements = true,  IsActive = true },

            // ── Nivel 3/4: Coopealianza (hijos de Activo No Corriente) ───────
            new Account { IdAccount = 36, CodeAccount = "1.2.01",    NameAccount = "Coopealianza",                                                                            TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 8,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 37, CodeAccount = "1.2.01.01", NameAccount = "Coopealianza - Aporte al Patrimonio CR02081300010008440263 (₡) - Baltodano Cubillo Ezequiel", TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 36, AllowsMovements = true,  IsActive = true },

            // ── Nivel 3/4: CCSS - Fondo de Pensión IVM (Activo No Corriente) ─
            new Account { IdAccount = 48, CodeAccount = "1.2.02",    NameAccount = "CCSS - Fondo de Pensión (IVM)",                                                   TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 8,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 49, CodeAccount = "1.2.02.01", NameAccount = "CCSS - IVM Trabajador - Baltodano Cubillo Ezequiel",                              TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 48, AllowsMovements = true,  IsActive = true },

            // ── Nivel 3/4: Banco Popular - LPT (Activo No Corriente) ─────────
            new Account { IdAccount = 50, CodeAccount = "1.2.03",    NameAccount = "Banco Popular - LPT (Fondo Capitalización Laboral)",                             TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 8,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 51, CodeAccount = "1.2.03.01", NameAccount = "Banco Popular - LPT - Baltodano Cubillo Ezequiel",                               TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 50, AllowsMovements = true,  IsActive = true },

            // ── Nivel 2: Pasivo ────────────────────────────────────────────────
            new Account { IdAccount = 9,  CodeAccount = "2.1", NameAccount = "Pasivo Corriente",       TypeAccount = "Pasivo",  LevelAccount = 2, IdAccountParent = 2, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 10, CodeAccount = "2.2", NameAccount = "Pasivo No Corriente",    TypeAccount = "Pasivo",  LevelAccount = 2, IdAccountParent = 2, AllowsMovements = false, IsActive = true },

            // ── Nivel 3: Tarjetas BAC Credomatic (hijos de Pasivo Corriente) ──
            new Account { IdAccount = 28, CodeAccount = "2.1.01",    NameAccount = "BAC Credomatic - Tarjetas",                TypeAccount = "Pasivo", LevelAccount = 3, IdAccountParent = 9,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 29, CodeAccount = "2.1.01.01", NameAccount = "BAC - AMEX ****-8052 - Baltodano Cubillo Ezequiel",  TypeAccount = "Pasivo", LevelAccount = 4, IdAccountParent = 28, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 30, CodeAccount = "2.1.01.02", NameAccount = "BAC - MCARD ****-6515 - Baltodano Cubillo Ezequiel", TypeAccount = "Pasivo", LevelAccount = 4, IdAccountParent = 28, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 31, CodeAccount = "2.1.01.03", NameAccount = "BAC - MCARD ****-8608 - Baltodano Cubillo Ezequiel", TypeAccount = "Pasivo", LevelAccount = 4, IdAccountParent = 28, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 32, CodeAccount = "2.1.01.04", NameAccount = "BAC - VISA ****-1593 - Baltodano Cubillo Ezequiel",  TypeAccount = "Pasivo", LevelAccount = 4, IdAccountParent = 28, AllowsMovements = true,  IsActive = true },

            // ── Nivel 3: Coopealianza - Préstamos (Pasivo Corriente) ──────────
            new Account { IdAccount = 40, CodeAccount = "2.1.02",    NameAccount = "Coopealianza - Préstamos",                                                              TypeAccount = "Pasivo", LevelAccount = 3, IdAccountParent = 9,  AllowsMovements = false, IsActive = true },

            // ── Nivel 3/4: Adelantos Salariales (Pasivo Corriente) ───────────
            new Account { IdAccount = 52, CodeAccount = "2.1.03",    NameAccount = "Adelantos Salariales por Liquidar",                                               TypeAccount = "Pasivo", LevelAccount = 3, IdAccountParent = 9,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 53, CodeAccount = "2.1.03.01", NameAccount = "Adelanto Salarial ITQS - Baltodano Cubillo Ezequiel",                            TypeAccount = "Pasivo", LevelAccount = 4, IdAccountParent = 52, AllowsMovements = true,  IsActive = true },

            // ── Nivel 3/4: Coopealianza - Préstamos (Pasivo No Corriente) ─────
            new Account { IdAccount = 41, CodeAccount = "2.2.01",    NameAccount = "Coopealianza - Préstamos",                                                              TypeAccount = "Pasivo", LevelAccount = 3, IdAccountParent = 10, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 42, CodeAccount = "2.2.01.01", NameAccount = "Coopealianza - Préstamo CR05081302810003488995 (₡) - Baltodano Cubillo Ezequiel",       TypeAccount = "Pasivo", LevelAccount = 4, IdAccountParent = 41, AllowsMovements = true,  IsActive = true },

            // ── Nivel 2: Capital ───────────────────────────────────────────────
            new Account { IdAccount = 11, CodeAccount = "3.1", NameAccount = "Utilidad Acumulada",     TypeAccount = "Capital", LevelAccount = 2, IdAccountParent = 3, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 12, CodeAccount = "3.2", NameAccount = "Utilidad del Período",   TypeAccount = "Capital", LevelAccount = 2, IdAccountParent = 3, AllowsMovements = true,  IsActive = true },

            // ── Nivel 2: Ingreso familiar ─────────────────────────────────────
            new Account { IdAccount = 13, CodeAccount = "4.1", NameAccount = "Salario / Sueldos",        TypeAccount = "Ingreso", LevelAccount = 2, IdAccountParent = 4, AllowsMovements = false, IsActive = true },

            // ── Nivel 3/4: IT Quest Solutions (ITQS) ─────────────────────────
            new Account { IdAccount = 43, CodeAccount = "4.1.01",    NameAccount = "IT Quest Solutions (ITQS)",                                                      TypeAccount = "Ingreso", LevelAccount = 3, IdAccountParent = 13, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 44, CodeAccount = "4.1.01.01", NameAccount = "ITQS - Salario Ordinario Mensual CLS - Baltodano Cubillo Ezequiel",              TypeAccount = "Ingreso", LevelAccount = 4, IdAccountParent = 43, AllowsMovements = true,  IsActive = true },

            new Account { IdAccount = 14, CodeAccount = "4.2", NameAccount = "Servicios Profesionales",  TypeAccount = "Ingreso", LevelAccount = 2, IdAccountParent = 4, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 15, CodeAccount = "4.3", NameAccount = "Otros Ingresos",           TypeAccount = "Ingreso", LevelAccount = 2, IdAccountParent = 4, AllowsMovements = false, IsActive = true },

            // ── 4.4 Ingresos Financieros ──────────────────────────────────────
            new Account { IdAccount = 101, CodeAccount = "4.4",    NameAccount = "Ingresos Financieros",               TypeAccount = "Ingreso", LevelAccount = 2, IdAccountParent = 4,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 102, CodeAccount = "4.4.01", NameAccount = "Diferencial Cambiario Favorable",     TypeAccount = "Ingreso", LevelAccount = 3, IdAccountParent = 101, AllowsMovements = true,  IsActive = true },

            // ── 5.1 Cargas Sociales e Impuestos ──────────────────────────────
            new Account { IdAccount = 45, CodeAccount = "5.1",    NameAccount = "Cargas Sociales e Impuestos", TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,  AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 46, CodeAccount = "5.1.01", NameAccount = "Impuesto de Renta",           TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 45, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 47, CodeAccount = "5.1.02", NameAccount = "CCSS - SEM Trabajador",       TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 45, AllowsMovements = true,  IsActive = true },

            // ── 5.2 Vivienda ──────────────────────────────────────────────────
            new Account { IdAccount = 59,  CodeAccount = "5.2",     NameAccount = "Vivienda",                           TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 60,  CodeAccount = "5.2.01",  NameAccount = "Alquiler",                           TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 59,  AllowsMovements = true,  IsActive = true },

            // ── 5.3 Alimentación ──────────────────────────────────────────────
            new Account { IdAccount = 90,  CodeAccount = "5.3",     NameAccount = "Alimentación",                       TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 61,  CodeAccount = "5.3.01",  NameAccount = "Alimentación",                       TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 90,  AllowsMovements = true,  IsActive = true },

            // ── 5.4 Transporte ────────────────────────────────────────────────
            new Account { IdAccount = 91,  CodeAccount = "5.4",     NameAccount = "Transporte",                         TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 62,  CodeAccount = "5.4.01",  NameAccount = "Gasolina",                           TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 91,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 83,  CodeAccount = "5.4.02",  NameAccount = "Transporte Actividades",             TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 91,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 84,  CodeAccount = "5.4.03",  NameAccount = "Transporte Citas",                   TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 91,  AllowsMovements = true,  IsActive = true },

            // ── 5.5 Finanzas ──────────────────────────────────────────────────
            new Account { IdAccount = 92,  CodeAccount = "5.5",     NameAccount = "Finanzas",                           TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 63,  CodeAccount = "5.5.01",  NameAccount = "Tarjeta BN",                         TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 92,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 64,  CodeAccount = "5.5.02",  NameAccount = "Tarjeta BAC Tasa Cero",              TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 92,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 65,  CodeAccount = "5.5.03",  NameAccount = "Tarjeta BAC Préstamo 2M",            TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 92,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 66,  CodeAccount = "5.5.04",  NameAccount = "Coopealianza",                       TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 92,  AllowsMovements = true,  IsActive = true },

            // ── 5.6 Educación ─────────────────────────────────────────────────
            new Account { IdAccount = 93,  CodeAccount = "5.6",     NameAccount = "Educación",                          TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 67,  CodeAccount = "5.6.01",  NameAccount = "Clases de Inglés",                   TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 93,  AllowsMovements = true,  IsActive = true },

            // ── 5.7 Comunicaciones ────────────────────────────────────────────
            new Account { IdAccount = 94,  CodeAccount = "5.7",     NameAccount = "Comunicaciones",                     TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 68,  CodeAccount = "5.7.01",  NameAccount = "Teléfono Celular",                   TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 94,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 79,  CodeAccount = "5.7.02",  NameAccount = "Internet",                           TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 94,  AllowsMovements = true,  IsActive = true },

            // ── 5.8 Suscripciones ─────────────────────────────────────────────
            new Account { IdAccount = 95,  CodeAccount = "5.8",     NameAccount = "Suscripciones",                      TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 69,  CodeAccount = "5.8.01",  NameAccount = "Netflix",                            TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 95,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 70,  CodeAccount = "5.8.02",  NameAccount = "App Anime",                          TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 95,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 71,  CodeAccount = "5.8.03",  NameAccount = "Apple Music",                        TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 95,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 72,  CodeAccount = "5.8.04",  NameAccount = "Apple iCloud",                       TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 95,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 73,  CodeAccount = "5.8.05",  NameAccount = "ChatGPT",                            TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 95,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 74,  CodeAccount = "5.8.06",  NameAccount = "Copilot",                            TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 95,  AllowsMovements = true,  IsActive = true },

            // ── 5.9 Servicios del Hogar ───────────────────────────────────────
            new Account { IdAccount = 97,  CodeAccount = "5.9",     NameAccount = "Servicios del Hogar",                TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 80,  CodeAccount = "5.9.01",  NameAccount = "AyA",                                TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 97,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 81,  CodeAccount = "5.9.02",  NameAccount = "CNFL",                               TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 97,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 82,  CodeAccount = "5.9.03",  NameAccount = "Teléfono Casa",                      TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 97,  AllowsMovements = true,  IsActive = true },

            // ── 5.10 Personal y Hogar ─────────────────────────────────────────
            new Account { IdAccount = 99,  CodeAccount = "5.10",    NameAccount = "Personal y Hogar",                   TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 77,  CodeAccount = "5.10.01", NameAccount = "Aporte Familiar",                    TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 99,  AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 85,  CodeAccount = "5.10.02", NameAccount = "Ayuda en Casa",                      TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 99,  AllowsMovements = true,  IsActive = true },

            // ── 5.11 Obligaciones ─────────────────────────────────────────────
            new Account { IdAccount = 100, CodeAccount = "5.11",    NameAccount = "Obligaciones",                       TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 86,  CodeAccount = "5.11.01", NameAccount = "Municipalidad",                      TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 100, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 87,  CodeAccount = "5.11.02", NameAccount = "Campo Santo",                        TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 100, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 88,  CodeAccount = "5.11.03", NameAccount = "Campo Santo Mantenimiento",          TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 100, AllowsMovements = true,  IsActive = true },

            // ── 5.12 Otros ────────────────────────────────────────────────────
            new Account { IdAccount = 96,  CodeAccount = "5.12",    NameAccount = "Otros",                              TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 75,  CodeAccount = "5.12.01", NameAccount = "Gastos en Pareja",                   TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 96,  AllowsMovements = true,  IsActive = true },

            // ── 1.1.06 Caja / Efectivo ─────────────────────────────────────
            new Account { IdAccount = 105, CodeAccount = "1.1.06",    NameAccount = "Caja / Efectivo",  TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 7,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 106, CodeAccount = "1.1.06.01", NameAccount = "Caja CRC (₡)",    TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 105, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 107, CodeAccount = "1.1.06.02", NameAccount = "Caja USD ($)",    TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 105, AllowsMovements = true,  IsActive = true },

            // ── 5.13 Gastos Financieros ───────────────────────────────────────
            new Account { IdAccount = 103, CodeAccount = "5.13",    NameAccount = "Gastos Financieros",                 TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 104, CodeAccount = "5.13.01", NameAccount = "Diferencial Cambiario Desfavorable", TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 103, AllowsMovements = true,  IsActive = true },

            // ── 1.1.07 Inventario ─────────────────────────────────────────────
            new Account { IdAccount = 108, CodeAccount = "1.1.07",    NameAccount = "Inventario",                TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 7,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 109, CodeAccount = "1.1.07.01", NameAccount = "Inventario de Mercadería", TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 108, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 110, CodeAccount = "1.1.07.02", NameAccount = "Materias Primas",          TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 108, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 111, CodeAccount = "1.1.07.03", NameAccount = "Productos en Proceso",     TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 108, AllowsMovements = true,  IsActive = true },

            // ── 5.14 Ajustes de Inventario ────────────────────────────────────
            new Account { IdAccount = 112, CodeAccount = "5.14",    NameAccount = "Ajustes de Inventario",          TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 113, CodeAccount = "5.14.01", NameAccount = "Faltantes de Inventario (Merma)", TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 112, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 114, CodeAccount = "5.14.02", NameAccount = "Sobrantes de Inventario",         TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 112, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 115, CodeAccount = "5.14.03", NameAccount = "Costos de Producción",            TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 112, AllowsMovements = true,  IsActive = true },

            // ── 4.5 Ingresos por Ventas (módulo de ventas) ───────────────────
            new Account { IdAccount = 116, CodeAccount = "4.5",     NameAccount = "Ingresos por Ventas",              TypeAccount = "Ingreso", LevelAccount = 2, IdAccountParent = 4,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 117, CodeAccount = "4.5.01",  NameAccount = "Ingresos por Ventas — Mercadería", TypeAccount = "Ingreso", LevelAccount = 3, IdAccountParent = 116, AllowsMovements = true,  IsActive = true },

            // ── 5.15 Costo de Ventas (módulo de ventas) ──────────────────────
            new Account { IdAccount = 118, CodeAccount = "5.15",    NameAccount = "Costo de Ventas",                  TypeAccount = "Gasto", LevelAccount = 2, IdAccountParent = 5,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 119, CodeAccount = "5.15.01", NameAccount = "Costo de Ventas — Mercadería",     TypeAccount = "Gasto", LevelAccount = 3, IdAccountParent = 118, AllowsMovements = true,  IsActive = true },

            // ── 1.1.08 Cuentas por Cobrar (módulo de ventas a crédito) ───────────
            new Account { IdAccount = 120, CodeAccount = "1.1.08",    NameAccount = "Cuentas por Cobrar",              TypeAccount = "Activo", LevelAccount = 3, IdAccountParent = 7,   AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 121, CodeAccount = "1.1.08.01", NameAccount = "Cuentas por Cobrar — Clientes CRC (₡)", TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 120, AllowsMovements = true,  IsActive = true },
            new Account { IdAccount = 122, CodeAccount = "1.1.08.02", NameAccount = "Cuentas por Cobrar — Clientes USD ($)", TypeAccount = "Activo", LevelAccount = 4, IdAccountParent = 120, AllowsMovements = true,  IsActive = true }
        );
    }
}
