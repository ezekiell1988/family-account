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

        // ── Seed: 6 cuentas raíz del plan contable básico ─────
        builder.HasData(
            new Account { IdAccount = 1, CodeAccount = "1", NameAccount = "Activo",  TypeAccount = "Activo",  LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 2, CodeAccount = "2", NameAccount = "Pasivo",  TypeAccount = "Pasivo",  LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 3, CodeAccount = "3", NameAccount = "Capital", TypeAccount = "Capital", LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 4, CodeAccount = "4", NameAccount = "Ingreso", TypeAccount = "Ingreso", LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 5, CodeAccount = "5", NameAccount = "Gasto",   TypeAccount = "Gasto",   LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true },
            new Account { IdAccount = 6, CodeAccount = "6", NameAccount = "Control", TypeAccount = "Control", LevelAccount = 1, IdAccountParent = null, AllowsMovements = false, IsActive = true }
        );
    }
}
