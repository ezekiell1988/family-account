using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using FamilyAccountApi.BackgroundJobs;
using FamilyAccountApi.Infrastructure.Data;
using FamilyAccountApi.Infrastructure.Options;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        // ─── Azure Key Vault (producción) ─────────────────────────────────────
        var keyVaultUri = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URI");
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
            builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());

        var cfg = builder.Configuration;

        // ─── Connection Strings ───────────────────────────────────────────────
        var dbConnectionString = cfg.RequireConnectionString(
            "Db:ConnectionString", "DbBase64", "DB_CONNECTION_STRING_BASE64");
        var redisConnectionString = cfg.RequireConnectionString(
            "Redis:ConnectionString", "RedisBase64", "REDIS_CONNECTION_STRING_BASE64");

        // ─── Options ─────────────────────────────────────────────────────────
        builder.Services.AddOptions<AppOptions>()
            .BindConfiguration(AppOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

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

        // ─── Entity Framework Core ────────────────────────────────────────────
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(dbConnectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3)));

        // ─── Redis Distributed Cache ──────────────────────────────────────────
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "FamilyAccount:";
        });

        // ─── Hangfire ────────────────────────────────────────────────────────
        var hangfireOpts = cfg
            .GetSection(HangfireOptions.Section)
            .Get<HangfireOptions>() ?? new HangfireOptions();

        GlobalJobFilters.Filters.Add(
            new AutomaticRetryAttribute { Attempts = hangfireOpts.AutomaticRetryAttempts });

        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(dbConnectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout       = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout   = TimeSpan.FromMinutes(5),
                QueuePollInterval            = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks           = true
            }));

        builder.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireOpts.WorkerCount;
            options.Queues      = hangfireOpts.Queues;
        });

        // ─── Background Job registrations ────────────────────────────────────
        builder.Services.AddScoped<EmailJobs>();
        builder.Services.AddScoped<PinJobs>();
        builder.Services.AddScoped<FiscalPeriodJobs>();
        builder.Services.AddScoped<BankStatementImportJob>();
        builder.Services.AddScoped<FinancialObligationBacTcSyncJob>();

        return builder;
    }
}
