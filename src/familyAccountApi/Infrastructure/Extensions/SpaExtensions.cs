using Microsoft.Extensions.FileProviders;

namespace FamilyAccountApi.Infrastructure.Extensions;

public static class SpaExtensions
{
    /// <summary>
    /// Registra los archivos estáticos de la SPA Angular y el fallback a index.html.
    /// Las rutas que inicien con /api las ignora y pasan al pipeline de endpoints.
    /// </summary>
    public static WebApplication UseSpa(this WebApplication app, string spaRoot)
    {
        if (!Directory.Exists(spaRoot))
            return app;

        // Archivos estáticos solo para rutas que NO son /api.
        app.UseWhen(
            ctx => !ctx.Request.Path.StartsWithSegments("/api"),
            branch => branch.UseStaticFiles(
                new StaticFileOptions { FileProvider = new PhysicalFileProvider(spaRoot) }));

        // Fallback: rutas /api sin endpoint → 404; resto → index.html (client-side routing).
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

        return app;
    }
}
