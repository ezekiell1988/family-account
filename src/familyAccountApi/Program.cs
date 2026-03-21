using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using FamilyAccountApi.BackgroundJobs;
using FamilyAccountApi.Features.AccountingEntries;
using FamilyAccountApi.Features.Accounts;
using FamilyAccountApi.Features.Auth;
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
using FamilyAccountApi.Features.Products;
using FamilyAccountApi.Features.ProductSKUs;
using FamilyAccountApi.Features.Users;
using FamilyAccountApi.Hangfire;
using FamilyAccountApi.Infrastructure.Extensions;
using FamilyAccountApi.Infrastructure.Options;
using FamilyAccountApi.OpenApi;
using Hangfire;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Azure Key Vault (producción) ────────────────────────────────────────────
var keyVaultUri = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URI");
if (!string.IsNullOrWhiteSpace(keyVaultUri))
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());

// ─── Infraestructura (Options + EF Core + Redis + Hangfire) ──────────────────
builder.AddInfrastructure();

// ─── Seguridad (JWT Bearer + Authorization) ───────────────────────────────────
builder.Services.AddJwtSecurity(builder.Configuration);

// ─── OpenAPI + Scalar ────────────────────────────────────────────────────────
builder.Services.AddOpenApi("v1", options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

// ─── Cross-cutting services ───────────────────────────────────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

// ─── Módulos de features ─────────────────────────────────────────────────────
builder.Services
    .AddUsersModule()
    .AddAuthModule()
    .AddEmailModule()
    .AddProductSKUsModule()
    .AddProductsModule()
    .AddProductCategoriesModule()
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
    .AddBankStatementTransactionsModule();

// ─── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ─── Pipeline (orden importa) ─────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("AllowAll");

// ─── SPA Angular (archivos estáticos desde src/familyAccountWeb/www) ──────────
var spaRoot = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "familyAccountWeb", "www"));

if (Directory.Exists(spaRoot))
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(spaRoot) });

// ─── Hangfire Dashboard ───────────────────────────────────────────────────────
var hangfireOpts = app.Services.GetRequiredService<IOptions<HangfireOptions>>().Value;
app.UseHangfireDashboard(hangfireOpts.DashboardPath, new DashboardOptions
{
    Authorization = [new HangfireBasicAuthFilter()],
    DashboardTitle = "Family Account — Jobs"
});

app.UseAuthentication();
app.UseAuthorization();

// ─── Scalar / OpenAPI (solo en Development) ──────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Family Account API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.ShowSidebar = true;
    });
}

// ─── Health (fuera del grupo /api/v1) ────────────────────────────────────────
app.MapHealthEndpoints();

// ─── Endpoints v1 ────────────────────────────────────────────────────────────
var v1 = app.MapGroup("/api/v1").WithGroupName("v1");

v1.MapUsersEndpoints();
v1.MapAuthEndpoints();
v1.MapProductSKUsEndpoints();
v1.MapProductsEndpoints();
v1.MapProductCategoriesEndpoints();
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

// ─── Recurring jobs ───────────────────────────────────────────────────────────
RecurringJob.AddOrUpdate<FiscalPeriodJobs>(
    "create-fiscal-year-periods",
    job => job.CreateCurrentYearPeriodsAsync(),
    "0 3 1 1 *");

// ─── SPA Fallback ─────────────────────────────────────────────────────────────
if (Directory.Exists(spaRoot))
{
    app.MapFallback(async ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.SendFileAsync(Path.Combine(spaRoot, "index.html"));
    });
}

app.Run();

