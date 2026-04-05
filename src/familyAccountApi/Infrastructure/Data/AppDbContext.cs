using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Infrastructure.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>     User     => Set<User>();
    public DbSet<UserPin>  UserPin  => Set<UserPin>();
    public DbSet<Role>     Role     => Set<Role>();
    public DbSet<UserRole> UserRole => Set<UserRole>();
    public DbSet<Contact>            Contact            => Set<Contact>();
    public DbSet<ContactType>         ContactType        => Set<ContactType>();
    public DbSet<ContactContactType>  ContactContactType => Set<ContactContactType>();
    public DbSet<Product>                  Product                  => Set<Product>();
    public DbSet<ProductCategory>          ProductCategory          => Set<ProductCategory>();
    public DbSet<ProductProductCategory>   ProductProductCategory   => Set<ProductProductCategory>();
    public DbSet<UnitOfMeasure>            UnitOfMeasure            => Set<UnitOfMeasure>();
    public DbSet<UnitType>                 UnitType                 => Set<UnitType>();
    public DbSet<ProductType>              ProductType              => Set<ProductType>();
    public DbSet<ProductUnit>              ProductUnit              => Set<ProductUnit>();
    public DbSet<ProductRecipe>            ProductRecipe            => Set<ProductRecipe>();
    public DbSet<ProductRecipeLine>        ProductRecipeLine        => Set<ProductRecipeLine>();
    public DbSet<InventoryLot>             InventoryLot             => Set<InventoryLot>();
    public DbSet<InventoryAdjustmentType>  InventoryAdjustmentType  => Set<InventoryAdjustmentType>();
    public DbSet<InventoryAdjustment>      InventoryAdjustment      => Set<InventoryAdjustment>();
    public DbSet<InventoryAdjustmentLine>  InventoryAdjustmentLine  => Set<InventoryAdjustmentLine>();
    public DbSet<InventoryAdjustmentEntry> InventoryAdjustmentEntry => Set<InventoryAdjustmentEntry>();
    public DbSet<ProductionSnapshot>       ProductionSnapshot       => Set<ProductionSnapshot>();
    public DbSet<ProductionSnapshotLine>   ProductionSnapshotLine   => Set<ProductionSnapshotLine>();
    public DbSet<Account>                  Account                  => Set<Account>();
    public DbSet<FiscalPeriod>             FiscalPeriod             => Set<FiscalPeriod>();
    public DbSet<AccountingEntry>          AccountingEntry          => Set<AccountingEntry>();
    public DbSet<AccountingEntryLine>      AccountingEntryLine      => Set<AccountingEntryLine>();
    public DbSet<CostCenter>               CostCenter               => Set<CostCenter>();
    public DbSet<Currency>                 Currency                 => Set<Currency>();
    public DbSet<ExchangeRate>             ExchangeRate             => Set<ExchangeRate>();
    public DbSet<Budget>                   Budget                   => Set<Budget>();
    public DbSet<Bank>                     Bank                     => Set<Bank>();
    public DbSet<BankAccount>              BankAccount              => Set<BankAccount>();
    public DbSet<BankMovementType>         BankMovementType         => Set<BankMovementType>();
    public DbSet<BankMovement>             BankMovement             => Set<BankMovement>();
    public DbSet<BankMovementDocument>     BankMovementDocument     => Set<BankMovementDocument>();
    public DbSet<BankStatementTemplate>    BankStatementTemplate    => Set<BankStatementTemplate>();
    public DbSet<BankStatementImport>      BankStatementImport      => Set<BankStatementImport>();
    public DbSet<BankStatementTransaction> BankStatementTransaction => Set<BankStatementTransaction>();
    public DbSet<ProductAccount>           ProductAccount           => Set<ProductAccount>();
    public DbSet<ProductOptionGroup>       ProductOptionGroup       => Set<ProductOptionGroup>();
    public DbSet<ProductOptionItem>        ProductOptionItem        => Set<ProductOptionItem>();
    public DbSet<ProductComboSlot>         ProductComboSlot         => Set<ProductComboSlot>();
    public DbSet<ProductComboSlotProduct>  ProductComboSlotProduct  => Set<ProductComboSlotProduct>();
    public DbSet<PriceList>                PriceList                => Set<PriceList>();
    public DbSet<PriceListItem>            PriceListItem            => Set<PriceListItem>();
    public DbSet<SalesOrder>               SalesOrder               => Set<SalesOrder>();
    public DbSet<SalesOrderLine>           SalesOrderLine           => Set<SalesOrderLine>();
    public DbSet<SalesOrderLineFulfillment> SalesOrderLineFulfillment => Set<SalesOrderLineFulfillment>();
    public DbSet<SalesOrderAdvance>        SalesOrderAdvance        => Set<SalesOrderAdvance>();
    public DbSet<ProductionOrder>          ProductionOrder          => Set<ProductionOrder>();
    public DbSet<ProductionOrderLine>      ProductionOrderLine      => Set<ProductionOrderLine>();
    public DbSet<PurchaseInvoiceType>      PurchaseInvoiceType      => Set<PurchaseInvoiceType>();
    public DbSet<PurchaseInvoice>          PurchaseInvoice          => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine>      PurchaseInvoiceLine      => Set<PurchaseInvoiceLine>();
    public DbSet<PurchaseInvoiceEntry>     PurchaseInvoiceEntry     => Set<PurchaseInvoiceEntry>();
    public DbSet<PurchaseInvoiceLineEntry> PurchaseInvoiceLineEntry => Set<PurchaseInvoiceLineEntry>();
    public DbSet<SalesInvoiceType>         SalesInvoiceType         => Set<SalesInvoiceType>();
    public DbSet<SalesInvoice>             SalesInvoice             => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine>            SalesInvoiceLine            => Set<SalesInvoiceLine>();
    public DbSet<SalesInvoiceLineBomDetail>   SalesInvoiceLineBomDetail   => Set<SalesInvoiceLineBomDetail>();
    public DbSet<SalesInvoiceEntry>           SalesInvoiceEntry           => Set<SalesInvoiceEntry>();
    public DbSet<SalesInvoiceLineEntry>       SalesInvoiceLineEntry       => Set<SalesInvoiceLineEntry>();
    public DbSet<Company>                  Company                  => Set<Company>();
    public DbSet<CompanyDomain>            CompanyDomain            => Set<CompanyDomain>();
    public DbSet<CompanyWhatsapp>          CompanyWhatsapp          => Set<CompanyWhatsapp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Aplicar convención camelCase a tablas y columnas
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.GetTableName() is { } tableName)
                entityType.SetTableName(ToCamelCase(tableName));

            foreach (var property in entityType.GetProperties())
            {
                var colName = property.GetColumnName();
                if (!string.IsNullOrEmpty(colName))
                    property.SetColumnName(ToCamelCase(colName));
            }
        }
    }

    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
