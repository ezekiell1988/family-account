using FamilyAccountApi.Features.AccountingEntries;
using FamilyAccountApi.Features.Accounts;
using FamilyAccountApi.Features.Auth;
using FamilyAccountApi.Features.Contacts;
using FamilyAccountApi.Features.BankAccounts;
using FamilyAccountApi.Features.BankMovements;
using FamilyAccountApi.Features.BankMovementTypes;
using FamilyAccountApi.Features.Banks;
using FamilyAccountApi.Features.BankStatementImports;
using FamilyAccountApi.Features.BankStatementTemplates;
using FamilyAccountApi.Features.BankStatementTransactions;
using FamilyAccountApi.Features.Budgets;
using FamilyAccountApi.Features.CostCenters;
using FamilyAccountApi.Features.Currencies;
using FamilyAccountApi.Features.Email;
using FamilyAccountApi.Features.ExchangeRates;
using FamilyAccountApi.Features.FiscalPeriods;
using FamilyAccountApi.Features.Health;
using FamilyAccountApi.Features.ProductCategories;
using FamilyAccountApi.Features.ProductAttributes;
using FamilyAccountApi.Features.Products;
using FamilyAccountApi.Features.ProductAccounts;
using FamilyAccountApi.Features.ProductTypes;
using FamilyAccountApi.Features.ProductUnits;
using FamilyAccountApi.Features.ProductRecipes;
using FamilyAccountApi.Features.InventoryLots;
using FamilyAccountApi.Features.InventoryAdjustments;
using FamilyAccountApi.Features.InventoryAdjustmentTypes;
using FamilyAccountApi.Features.Warehouses;
using FamilyAccountApi.Features.UnitsOfMeasure;
using FamilyAccountApi.Features.PurchaseInvoiceTypes;
using FamilyAccountApi.Features.PurchaseInvoices;
using FamilyAccountApi.Features.Users;
using FamilyAccountApi.Features.ProductOptionGroups;
using FamilyAccountApi.Features.ProductComboSlots;
using FamilyAccountApi.Features.SalesInvoiceTypes;
using FamilyAccountApi.Features.SalesInvoices;
using FamilyAccountApi.Features.PriceLists;
using FamilyAccountApi.Features.SalesOrders;
using FamilyAccountApi.Features.ProductionOrders;
using FamilyAccountApi.Features.FinancialObligations;
using FamilyAccountApi.Features.FinancialStatements;

namespace FamilyAccountApi.Infrastructure.Extensions;

public static class FeaturesExtensions
{
    public static IServiceCollection AddAllFeaturesModules(this IServiceCollection services)
        => services
            .AddUsersModule()
            .AddAuthModule()
            .AddEmailModule()
            .AddUnitsOfMeasureModule()
            .AddProductTypesModule()
            .AddProductsModule()
            .AddProductAttributesModule()
            .AddProductCategoriesModule()
            .AddProductUnitsModule()
            .AddProductRecipesModule()
            .AddInventoryLotsModule()
            .AddInventoryAdjustmentTypesModule()
            .AddInventoryAdjustmentsModule()
            .AddWarehousesModule()
            .AddAccountsModule()
            .AddFiscalPeriodsModule()
            .AddAccountingEntriesModule()
            .AddCostCentersModule()
            .AddCurrenciesModule()
            .AddExchangeRatesModule()
            .AddBudgetsModule()
            .AddBanksModule()
            .AddBankAccountsModule()
            .AddBankMovementTypesModule()
            .AddBankMovementsModule()
            .AddBankStatementTemplatesModule()
            .AddBankStatementImportsModule()
            .AddBankStatementTransactionsModule()
            .AddProductAccountsModule()
            .AddPurchaseInvoiceTypesModule()
            .AddPurchaseInvoicesModule()
            .AddContactsModule()
            .AddProductOptionGroupsModule()
            .AddProductComboSlotsModule()
            .AddSalesInvoiceTypesModule()
            .AddSalesInvoicesModule()
            .AddPriceListsModule()
            .AddSalesOrdersModule()
            .AddProductionOrdersModule()
            .AddFinancialObligationsModule()
            .AddFinancialStatementsModule();

    public static WebApplication MapAllEndpoints(this WebApplication app)
    {
        // ─── Health (fuera del grupo /api/v1) ─────────────────────────────────
        app.MapHealthEndpoints();

        // ─── Endpoints v1 ─────────────────────────────────────────────────────
        var v1 = app.MapGroup("/api/v1").WithGroupName("v1");

        v1.MapUsersEndpoints();
        v1.MapAuthEndpoints();
        v1.MapUnitsOfMeasureEndpoints();
        v1.MapProductTypesEndpoints();
        v1.MapProductsEndpoints();
        v1.MapProductAttributesEndpoints();
        v1.MapProductCategoriesEndpoints();
        v1.MapProductUnitsEndpoints();
        v1.MapProductRecipesEndpoints();
        v1.MapInventoryLotsEndpoints();
        v1.MapInventoryAdjustmentTypesEndpoints();
        v1.MapInventoryAdjustmentsEndpoints();
        v1.MapWarehousesEndpoints();
        v1.MapAccountsEndpoints();
        v1.MapFiscalPeriodsEndpoints();
        v1.MapAccountingEntriesEndpoints();
        v1.MapCostCentersEndpoints();
        v1.MapCurrenciesEndpoints();
        v1.MapExchangeRatesEndpoints();
        v1.MapBudgetsEndpoints();
        v1.MapBanksEndpoints();
        v1.MapBankAccountsEndpoints();
        v1.MapBankMovementTypesEndpoints();
        v1.MapBankMovementsEndpoints();
        v1.MapBankStatementTemplatesEndpoints();
        v1.MapBankStatementImportsEndpoints();
        v1.MapBankStatementTransactionsEndpoints();
        v1.MapProductAccountsEndpoints();
        v1.MapPurchaseInvoiceTypesEndpoints();
        v1.MapPurchaseInvoicesEndpoints();
        v1.MapContactsEndpoints();
        v1.MapProductOptionGroupsEndpoints();
        v1.MapProductComboSlotsEndpoints();
        v1.MapSalesInvoiceTypesEndpoints();
        v1.MapSalesInvoicesEndpoints();
        v1.MapPriceListsEndpoints();
        v1.MapSalesOrdersEndpoints();
        v1.MapProductionOrdersEndpoints();
        v1.MapFinancialObligationsEndpoints();
        v1.MapFinancialStatementsEndpoints();

        return app;
    }
}
