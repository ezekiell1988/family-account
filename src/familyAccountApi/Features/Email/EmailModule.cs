using FamilyAccountApi.Features.Email;

namespace FamilyAccountApi.Features.Email;

public static class EmailModule
{
    public static IServiceCollection AddEmailModule(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, SmtpEmailService>();
        return services;
    }
}
