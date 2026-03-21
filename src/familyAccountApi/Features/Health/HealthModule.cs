using FamilyAccountApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FamilyAccountApi.Features.Health;

public static class HealthModule
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/{uuidApp?}", GetHealth)
            .WithName("GetHealth")
            .WithSummary("Estado del API y configuración de la aplicación")
            .AllowAnonymous();

        return app;
    }

    private static IResult GetHealth(
        string? uuidApp,
        HttpContext context,
        IOptions<AppOptions> opts,
        IHostEnvironment env)
    {
        // Si viene un UUID y el dispositivo aún no tiene cookie, la registramos
        if (!string.IsNullOrWhiteSpace(uuidApp))
        {
            var existingToken = context.Request.Cookies["device_token"];
            if (string.IsNullOrEmpty(existingToken))
            {
                context.Response.Cookies.Append("device_token", uuidApp, new CookieOptions
                {
                    HttpOnly = true,
                    Secure   = !env.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    MaxAge   = TimeSpan.FromDays(365)
                });
            }
        }

        var app = opts.Value;

        return Results.Ok(new
        {
            status         = "ok",
            nameCustomer   = app.NameCustomer,
            sloganCustomer = app.SloganCustomer,
            apiVersion     = app.Version
        });
    }
}
