using FamilyAccountApi.BackgroundJobs;
using FamilyAccountApi.Hangfire;
using FamilyAccountApi.Infrastructure.Options;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.Extensions.Options;

namespace FamilyAccountApi.Infrastructure.Extensions;

public static class HangfireAppExtensions
{
    public static WebApplication UseHangfireDashboard(this WebApplication app)
    {
        var opts = app.Services.GetRequiredService<IOptions<HangfireOptions>>().Value;
        app.UseHangfireDashboard(opts.DashboardPath, new DashboardOptions
        {
            Authorization = [new HangfireBasicAuthFilter()],
            DashboardTitle = "Family Account — Jobs"
        });
        return app;
    }

    public static WebApplication MapRecurringJobs(this WebApplication app)
    {
        RecurringJob.AddOrUpdate<FiscalPeriodJobs>(
            "create-fiscal-year-periods",
            job => job.CreateCurrentYearPeriodsAsync(),
            "0 3 1 1 *");
        return app;
    }
}
