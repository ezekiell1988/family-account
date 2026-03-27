using FamilyAccountApi.OpenApi;
using Scalar.AspNetCore;

namespace FamilyAccountApi.Infrastructure.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiDocs(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        return services;
    }

    public static WebApplication UseOpenApiDocs(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Family Account API")
                .WithTheme(ScalarTheme.Purple)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            options.ShowSidebar = true;
        });
        return app;
    }
}
