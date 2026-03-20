using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable(t =>
            t.HasComment("Cuentas bancarias vinculadas a cuentas contables y monedas para conciliación y control de efectivo."));

        builder.HasKey(ba => ba.IdBankAccount);
        builder.Property(ba => ba.IdBankAccount)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único autoincremental de la cuenta bancaria.");

        builder.Property(ba => ba.IdBank)
            .IsRequired()
            .HasComment("FK a la entidad bancaria (banco o cooperativa) a la que pertenece esta cuenta.");

        builder.Property(ba => ba.IdAccount)
            .IsRequired()
            .HasComment("FK a la cuenta contable que representa esta cuenta bancaria en el mayor.");

        builder.Property(ba => ba.IdCurrency)
            .IsRequired()
            .HasComment("FK a la moneda manejada por la cuenta bancaria.");

        builder.Property(ba => ba.CodeBankAccount)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Código interno único de la cuenta bancaria.");

        builder.Property(ba => ba.AccountNumber)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(false)
            .HasComment("Número de cuenta bancaria o IBAN.");

        builder.Property(ba => ba.AccountHolder)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Titular de la cuenta bancaria.");

        builder.Property(ba => ba.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica si la cuenta bancaria está activa para operaciones y conciliaciones.");

        builder.HasIndex(ba => ba.CodeBankAccount)
            .IsUnique()
            .HasDatabaseName("UQ_bankAccount_codeBankAccount");

        builder.HasIndex(ba => ba.AccountNumber)
            .IsUnique()
            .HasDatabaseName("UQ_bankAccount_accountNumber");

        builder.HasIndex(ba => ba.IdBank)
            .HasDatabaseName("IX_bankAccount_idBank");

        builder.HasIndex(ba => ba.IdAccount)
            .HasDatabaseName("IX_bankAccount_idAccount");

        builder.HasIndex(ba => ba.IdCurrency)
            .HasDatabaseName("IX_bankAccount_idCurrency");

        builder.HasOne(ba => ba.IdBankNavigation)
            .WithMany(b => b.BankAccounts)
            .HasForeignKey(ba => ba.IdBank)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ba => ba.IdAccountNavigation)
            .WithMany(a => a.BankAccounts)
            .HasForeignKey(ba => ba.IdAccount)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ba => ba.IdCurrencyNavigation)
            .WithMany(c => c.BankAccounts)
            .HasForeignKey(ba => ba.IdCurrency)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Seed ──────────────────────────────────────────────────────────────
        // IdCurrency: 1 = CRC (₡), 2 = USD ($)
        // IdBank:     1=BCR  2=BAC  3=BN  4=COOPEALIANZA  5=DAVIVIENDA  6=BPOPULAR
        // IdAccount:  ver AccountConfiguration seed (nivel 4)
        builder.HasData(
            // BCR ─────────────────────────────────────────────────────────────
            new BankAccount { IdBankAccount = 1,  IdBank = 1, IdAccount = 25, IdCurrency = 1, CodeBankAccount = "BCR-AHO-001",       AccountNumber = "07015202001294229652",  AccountHolder = "Soto Arce Karen Tatiana",           IsActive = true },

            // BAC Credomatic – cuenta de ahorros ──────────────────────────────
            new BankAccount { IdBankAccount = 2,  IdBank = 2, IdAccount = 27, IdCurrency = 1, CodeBankAccount = "BAC-AHO-001",       AccountNumber = "CR73010200009497305680", AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },

            // BAC Credomatic – tarjetas de crédito ────────────────────────────
            new BankAccount { IdBankAccount = 3,  IdBank = 2, IdAccount = 29, IdCurrency = 1, CodeBankAccount = "BAC-CC-AMEX-8052",  AccountNumber = "****-8052",             AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },
            new BankAccount { IdBankAccount = 4,  IdBank = 2, IdAccount = 30, IdCurrency = 1, CodeBankAccount = "BAC-CC-MC-6515",    AccountNumber = "****-6515",             AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },
            new BankAccount { IdBankAccount = 5,  IdBank = 2, IdAccount = 31, IdCurrency = 1, CodeBankAccount = "BAC-CC-MC-8608",    AccountNumber = "****-8608",             AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },
            new BankAccount { IdBankAccount = 6,  IdBank = 2, IdAccount = 32, IdCurrency = 1, CodeBankAccount = "BAC-CC-VISA-1593",  AccountNumber = "****-1593",             AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },

            // Banco Nacional ──────────────────────────────────────────────────
            new BankAccount { IdBankAccount = 7,  IdBank = 3, IdAccount = 34, IdCurrency = 1, CodeBankAccount = "BN-AHO-CRC-001",    AccountNumber = "CR86015100020019688637", AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },
            new BankAccount { IdBankAccount = 8,  IdBank = 3, IdAccount = 35, IdCurrency = 2, CodeBankAccount = "BN-AHO-USD-001",    AccountNumber = "CR06015107220020012339", AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },

            // Coopealianza – ahorros y aporte al patrimonio ───────────────────
            new BankAccount { IdBankAccount = 9,  IdBank = 4, IdAccount = 39, IdCurrency = 1, CodeBankAccount = "COOPEAL-AHO-001",   AccountNumber = "CR54081300210008440287", AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },
            new BankAccount { IdBankAccount = 10, IdBank = 4, IdAccount = 37, IdCurrency = 1, CodeBankAccount = "COOPEAL-PAT-001",   AccountNumber = "CR02081300010008440263", AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true },

            // Davivienda – cuenta de ahorros (nómina ITQS) ────────────────────
            new BankAccount { IdBankAccount = 11, IdBank = 5, IdAccount = 55, IdCurrency = 1, CodeBankAccount = "DAVIV-AHO-001",     AccountNumber = "CR98010401446613244113", AccountHolder = "Baltodano Cubillo Ezequiel",        IsActive = true }
        );
    }
}