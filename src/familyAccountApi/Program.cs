using System.Text;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using FamilyAccountApi.BackgroundJobs;
using Microsoft.Extensions.Caching.Distributed;
using FamilyAccountApi.Features.Auth;
using FamilyAccountApi.Features.AccountingEntries;
using FamilyAccountApi.Features.BankAccounts;
using FamilyAccountApi.Features.BankStatementImports;
using FamilyAccountApi.Features.BankStatementTemplates;
using FamilyAccountApi.Features.BankStatementTransactions;
using FamilyAccountApi.Features.Budgets;
using FamilyAccountApi.Features.CostCenters;
using FamilyAccountApi.Features.Currencies;
using FamilyAccountApi.Features.Email;
using FamilyAccountApi.Features.ExchangeRates;
using FamilyAccountApi.Features.Accounts;
using FamilyAccountApi.Features.FiscalPeriods;
using FamilyAccountApi.Features.ProductCategories;
using FamilyAccountApi.Features.Products;
using FamilyAccountApi.Features.ProductSKUs;
using FamilyAccountApi.Features.Users;
using FamilyAccountApi.Hangfire;
using FamilyAccountApi.Infrastructure.Data;
using FamilyAccountApi.Infrastructure.Options;
using FamilyAccountApi.OpenApi;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Azure Key Vault (producción) ────────────────────────────────────────────
// El contenedor debe tener la variable AZURE_KEYVAULT_URI y Managed Identity
// con el rol "Key Vault Secrets User" asignado.
var keyVaultUri = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URI");
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// ─── Resolución de connection strings ────────────────────────────────────────
// Prioridad: 1) Key Vault (plain text)  2) appsettings base64  3) env var base64
static string? TryDecodeBase64(string? value)
{
    if (value is null) return null;
    try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
    catch { return null; }
}

static string RequireConnectionString(IConfiguration config, string kvKey, string base64ConfigKey, string envVar)
    => config[kvKey]                                              // Key Vault (plain text)
        ?? TryDecodeBase64(config.GetConnectionString(base64ConfigKey))  // appsettings base64
        ?? TryDecodeBase64(Environment.GetEnvironmentVariable(envVar))   // env var base64
        ?? throw new InvalidOperationException(
            $"Connection string no encontrado. Configure '{kvKey}' en Key Vault, " +
            $"'ConnectionStrings:{base64ConfigKey}' en appsettings (base64) " +
            $"o la variable de entorno '{envVar}' en base64.");

var dbConnectionString    = RequireConnectionString(builder.Configuration, "Db:ConnectionString",    "DbBase64",    "DB_CONNECTION_STRING_BASE64");
var redisConnectionString = RequireConnectionString(builder.Configuration, "Redis:ConnectionString", "RedisBase64", "REDIS_CONNECTION_STRING_BASE64");

// ─── Options ────────────────────────────────────────────────────────────────
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration(SmtpOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<HangfireOptions>()
    .BindConfiguration(HangfireOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ─── Entity Framework Core ──────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(dbConnectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3)));

// ─── Redis Distributed Cache ────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "FamilyAccount:";
});

// ─── Hangfire ───────────────────────────────────────────────────────────────
var hangfireOpts = builder.Configuration
    .GetSection(HangfireOptions.Section)
    .Get<HangfireOptions>() ?? new HangfireOptions();

GlobalJobFilters.Filters.Add(
    new Hangfire.AutomaticRetryAttribute { Attempts = hangfireOpts.AutomaticRetryAttempts });

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(dbConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = hangfireOpts.WorkerCount;
    options.Queues = hangfireOpts.Queues;
});

// Registrar jobs de Hangfire en DI
builder.Services.AddScoped<EmailJobs>();
builder.Services.AddScoped<PinJobs>();
builder.Services.AddScoped<FiscalPeriodJobs>();

// ─── Auth (JWT Bearer) ───────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection(JwtOptions.Section);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Secret"]
                    ?? throw new InvalidOperationException("Jwt:Secret no configurado"))),
            ClockSkew = TimeSpan.Zero
        };

        // Verificar blacklist de JWTs revocados
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var cache = ctx.HttpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                var jti = ctx.Principal?.FindFirst(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

                if (jti is not null)
                {
                    var revoked = await cache.GetStringAsync($"revoked:{jti}");
                    if (revoked is not null)
                        ctx.Fail("Token revocado.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Acceso total: solo Developer
    options.AddPolicy("Developer", p => p.RequireRole("Developer"));
    // Acceso amplio: Developer + Admin
    options.AddPolicy("Admin",     p => p.RequireRole("Developer", "Admin"));
    // Acceso básico: todos los roles autenticados
    options.AddPolicy("User",      p => p.RequireRole("Developer", "Admin", "User"));
});

// ─── OpenAPI + Scalar ────────────────────────────────────────────────────────
builder.Services.AddOpenApi("v1", options =>
{
    // BearerSecuritySchemeTransformer es un DocumentTransformer (corre último),
    // por lo que sobrescribe el {} vacío que ASP.NET Core inyecta internamente.
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// ─── Problem Details (RFC 9457) ──────────────────────────────────────────────
builder.Services.AddProblemDetails();

// ─── Validation (.NET 10 built-in) ──────────────────────────────────────────
builder.Services.AddValidation();

// ─── Módulos de features ─────────────────────────────────────────────────────
builder.Services.AddUsersModule();
builder.Services.AddAuthModule();
builder.Services.AddEmailModule();
builder.Services.AddProductSKUsModule();
builder.Services.AddProductsModule();
builder.Services.AddProductCategoriesModule();
builder.Services.AddAccountsModule();
builder.Services.AddFiscalPeriodsModule();
builder.Services.AddAccountingEntriesModule();
builder.Services.AddCostCentersModule();
builder.Services.AddCurrenciesModule();
builder.Services.AddExchangeRatesModule();
builder.Services.AddBudgetsModule();
builder.Services.AddBankAccountsModule();
builder.Services.AddBankStatementTemplatesModule();
builder.Services.AddBankStatementImportsModule();
builder.Services.AddBankStatementTransactionsModule();

// ─── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ─── Pipeline (orden importa) ─────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Hangfire Dashboard con autenticación Basic Auth admin/12345
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

// ─── Endpoints v1 ────────────────────────────────────────────────────────────
var v1 = app.MapGroup("/api/v1")
    .WithGroupName("v1");

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
v1.MapBankAccountsEndpoints();
v1.MapBankStatementTemplatesEndpoints();
v1.MapBankStatementImportsEndpoints();
v1.MapBankStatementTransactionsEndpoints();

// ─── Recurring jobs ──────────────────────────────────────────────────────────
// Crea los 12 períodos del año en curso cada 1° de enero a las 3:00 AM UTC.
RecurringJob.AddOrUpdate<FiscalPeriodJobs>(
    "create-fiscal-year-periods",
    job => job.CreateCurrentYearPeriodsAsync(),
    "0 3 1 1 *");

app.Run();

