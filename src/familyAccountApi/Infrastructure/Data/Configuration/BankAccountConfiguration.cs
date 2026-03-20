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
    }
}