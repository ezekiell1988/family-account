using System.Text;
using FamilyAccountApi.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FamilyAccountApi.Infrastructure.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddJwtSecurity(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.Section);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = jwtSection["Issuer"],
                    ValidateAudience         = true,
                    ValidAudience            = jwtSection["Audience"],
                    ValidateLifetime         = true,
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
                            .GetRequiredService<IDistributedCache>();
                        var jti = ctx.Principal?
                            .FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                        if (jti is not null)
                        {
                            var revoked = await cache.GetStringAsync($"revoked:{jti}");
                            if (revoked is not null)
                                ctx.Fail("Token revocado.");
                        }
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Developer", p => p.RequireRole("Developer"));
            options.AddPolicy("Admin",     p => p.RequireRole("Developer", "Admin"));
            options.AddPolicy("User",      p => p.RequireRole("Developer", "Admin", "User"));
        });

        return services;
    }
}
